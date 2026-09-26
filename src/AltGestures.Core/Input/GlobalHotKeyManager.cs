using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AltGestures.Core.Interop;

namespace AltGestures.Core.Input;

/// <summary>
/// 在专用消息线程上注册、分发并注销全局热键。
/// </summary>
public sealed class GlobalHotKeyManager : IDisposable
{
    private const string WindowClassName = "AltGestures.HotKeyMessageWindow";

    [ThreadStatic]
    private static GlobalHotKeyManager? currentManager;

    private readonly ConcurrentDictionary<int, Action> callbacks = [];
    private readonly ConcurrentDictionary<HotKey, int> registrationsByHotKey = [];
    private readonly ConcurrentQueue<HotKeyCommand> commands = new();
    private readonly AutoResetEvent commandQueued = new(false);
    private readonly ManualResetEvent initialized = new(false);
    private readonly object lifetimeSyncRoot = new();
    private readonly User32.WindowProc windowProcedure;

    private Thread? workerThread;
    private uint workerThreadId;
    private nint window;
    private int nextHotKeyId;
    private int pauseResumeHotKeyId = -1;
    private Exception? workerException;
    private volatile bool disposed;

    /// <summary>
    /// 初始化全局热键管理器。消息线程会随首个热键注册创建。
    /// </summary>
    public GlobalHotKeyManager()
    {
        windowProcedure = ProcessWindowMessage;
    }

    /// <summary>
    /// 注册全局热键；相同组合会先注销旧注册。成功返回热键 ID，失败返回 -1。
    /// </summary>
    public int Register(HotKey hotKey, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ObjectDisposedException.ThrowIf(disposed, this);

        EnsureWorkerStarted();
        var id = Interlocked.Increment(ref nextHotKeyId);
        var succeeded = false;

        ExecuteOnWorker(() =>
        {
            if (registrationsByHotKey.TryGetValue(hotKey, out var previousId))
            {
                RemoveRegistration(previousId);
            }

            succeeded = User32.RegisterHotKey(window, id, (uint)hotKey.Modifiers, (uint)hotKey.Key);
            if (!succeeded)
            {
                return;
            }

            callbacks[id] = callback;
            registrationsByHotKey[hotKey] = id;
        });

        return succeeded ? id : -1;
    }

    /// <summary>
    /// 注册暂停/恢复热键，重复注册时会先注销旧热键。
    /// </summary>
    public int RegisterPauseResume(HotKey hotKey, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ObjectDisposedException.ThrowIf(disposed, this);

        EnsureWorkerStarted();
        var oldPauseResumeHotKeyId = pauseResumeHotKeyId;
        var id = Interlocked.Increment(ref nextHotKeyId);
        var succeeded = false;

        ExecuteOnWorker(() =>
        {
            if (oldPauseResumeHotKeyId >= 0)
            {
                RemoveRegistration(oldPauseResumeHotKeyId);
            }

            if (registrationsByHotKey.TryGetValue(hotKey, out var previousId))
            {
                RemoveRegistration(previousId);
            }

            succeeded = User32.RegisterHotKey(window, id, (uint)hotKey.Modifiers, (uint)hotKey.Key);
            if (!succeeded)
            {
                return;
            }

            callbacks[id] = callback;
            registrationsByHotKey[hotKey] = id;
        });

        if (!succeeded)
        {
            return -1;
        }

        pauseResumeHotKeyId = id;
        return id;
    }

    /// <summary>
    /// 注销指定热键 ID。
    /// </summary>
    public bool Unregister(int id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (id <= 0)
        {
            return false;
        }

        var unregistered = false;
        ExecuteOnWorker(() => unregistered = RemoveRegistration(id));
        if (unregistered && id == pauseResumeHotKeyId)
        {
            pauseResumeHotKeyId = -1;
        }

        return unregistered;
    }

    /// <summary>
    /// 注销暂停/恢复热键。
    /// </summary>
    public bool UnregisterPauseResume()
    {
        var id = pauseResumeHotKeyId;
        return id >= 0 && Unregister(id);
    }

    /// <summary>
    /// 注销全部热键并停止消息线程。
    /// </summary>
    public void Dispose()
    {
        var thread = workerThread;
        lock (lifetimeSyncRoot)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            thread = workerThread;
            workerThread = null;
        }

        if (thread is null)
        {
            commandQueued.Dispose();
            initialized.Dispose();
            return;
        }

        using var completed = new ManualResetEvent(false);
        var command = new HotKeyCommand(StopWorker, completed);
        commands.Enqueue(command);
        _ = User32.PostThreadMessage(workerThreadId, NativeConstants.WM_NULL, nint.Zero, nint.Zero);
        _ = command.Completed.WaitOne(TimeSpan.FromSeconds(3));
        thread.Join(TimeSpan.FromSeconds(3));
        commandQueued.Dispose();
        initialized.Dispose();
    }

    private static nint ProcessWindowMessage(nint windowHandle, uint message, nint wParam, nint lParam)
    {
        if (message == NativeConstants.WM_HOTKEY
            && currentManager?.callbacks.TryGetValue((int)wParam, out var callback) is true)
        {
            callback();
            return nint.Zero;
        }

        return User32.DefWindowProc(windowHandle, message, wParam, lParam);
    }

    private void EnsureWorkerStarted()
    {
        lock (lifetimeSyncRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (workerException is not null)
            {
                throw new InvalidOperationException("全局热键消息线程不可用。", workerException);
            }

            if (workerThread is not null)
            {
                return;
            }

            workerThread = new Thread(RunWorker)
            {
                IsBackground = true,
                Name = "AltGestures全局热键消息线程"
            };
            workerThread.Start();
        }

        if (!initialized.WaitOne(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("全局热键消息线程启动超时。");
        }

        if (workerException is not null)
        {
            throw new InvalidOperationException("全局热键消息窗口创建失败。", workerException);
        }
    }

    private void RunWorker()
    {
        currentManager = this;
        try
        {
            workerThreadId = Kernel32.GetCurrentThreadId();
            window = CreateMessageWindow();
        }
        catch (Exception exception)
        {
            workerException = exception;
        }
        finally
        {
            _ = initialized.Set();
        }

        if (window == nint.Zero)
        {
            return;
        }

        try
        {
            while (User32.GetMessage(out var message, nint.Zero, 0, 0) > 0)
            {
                ProcessCommands();
                _ = User32.TranslateMessage(ref message);
                _ = User32.DispatchMessage(ref message);
            }
        }
        catch (Exception exception)
        {
            workerException = exception;
        }
        finally
        {
            currentManager = null;
        }
    }

    private nint CreateMessageWindow()
    {
        var instance = Kernel32.GetModuleHandle(null);
        var windowClass = new WNDCLASSEX
        {
            Size = System.Runtime.CompilerServices.Unsafe.SizeOf<WNDCLASSEX>(),
            WindowProc = Marshal.GetFunctionPointerForDelegate(windowProcedure),
            Instance = instance,
            ClassName = WindowClassName
        };

        var atom = User32.RegisterClassEx(ref windowClass);
        if (atom == 0 && Marshal.GetLastWin32Error() != NativeConstants.ERROR_CLASS_ALREADY_EXISTS)
        {
            throw new InvalidOperationException($"注册全局热键窗口类失败，错误码 {Marshal.GetLastWin32Error()}。");
        }

        var createdWindow = User32.CreateWindowEx(
            0,
            WindowClassName,
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            NativeConstants.HWND_MESSAGE,
            nint.Zero,
            instance,
            nint.Zero);
        if (createdWindow == nint.Zero)
        {
            throw new InvalidOperationException($"创建全局热键消息窗口失败，错误码 {Marshal.GetLastWin32Error()}。");
        }

        return createdWindow;
    }

    private void ExecuteOnWorker(Action work)
    {
        using var completed = new ManualResetEvent(false);
        var command = new HotKeyCommand(work, completed);
        commands.Enqueue(command);
        if (!User32.PostThreadMessage(workerThreadId, NativeConstants.WM_NULL, nint.Zero, nint.Zero))
        {
            throw new InvalidOperationException($"唤醒全局热键消息线程失败，错误码 {Marshal.GetLastWin32Error()}。");
        }

        if (!command.Completed.WaitOne(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("等待全局热键注册命令超时。");
        }

        if (command.Exception is not null)
        {
            throw new InvalidOperationException("全局热键命令执行失败。", command.Exception);
        }
    }

    private void ProcessCommands()
    {
        while (commands.TryDequeue(out var command))
        {
            try
            {
                command.Work();
            }
            catch (Exception exception)
            {
                command.Exception = exception;
            }
            finally
            {
                _ = command.Completed.Set();
            }
        }
    }

    private bool RemoveRegistration(int id)
    {
        var unregistered = User32.UnregisterHotKey(window, id);
        if (!unregistered)
        {
            return false;
        }

        callbacks.TryRemove(id, out _);
        foreach (var pair in registrationsByHotKey)
        {
            if (pair.Value == id)
            {
                registrationsByHotKey.TryRemove(pair.Key, out _);
                break;
            }
        }

        return true;
    }

    private void StopWorker()
    {
        foreach (var id in callbacks.Keys.ToArray())
        {
            _ = User32.UnregisterHotKey(window, id);
        }

        callbacks.Clear();
        registrationsByHotKey.Clear();
        if (window != nint.Zero)
        {
            _ = User32.DestroyWindow(window);
            window = nint.Zero;
        }

        User32.PostQuitMessage(0);
    }

    private sealed class HotKeyCommand(Action work, ManualResetEvent completed)
    {
        public Action Work { get; } = work;

        public ManualResetEvent Completed { get; } = completed;

        public Exception? Exception { get; set; }
    }
}
