// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.ComponentModel;
using System.Runtime.InteropServices;
using AltGestures.Core.Input;
using AltGestures.Core.Interop;

namespace AltGestures.Core.Windows;

public sealed class MouseKeyboardHook : IMouseKeyboardHook
{
    private readonly ManualResetEventSlim installCompleted = new(false);
    private readonly User32.LowLevelHookProc mouseCallback;
    private readonly User32.LowLevelHookProc keyboardCallback;
    private Thread? hookThread;
    private nint mouseHook;
    private nint keyboardHook;
    private uint hookThreadId;
    private Exception? installException;
    private bool disposed;

    public MouseKeyboardHook()
    {
        mouseCallback = MouseHookProc;
        keyboardCallback = KeyboardHookProc;
    }

    public event Action<MouseHookEventArgs>? MouseHookEvent;
    public event Action<KeyboardHookEventArgs>? KeyboardHookEvent;

    public bool IsInstalled => hookThread is { IsAlive: true } && mouseHook != nint.Zero && keyboardHook != nint.Zero;

    public void Install()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (hookThread is not null)
        {
            throw new InvalidOperationException("钩子已经安装。");
        }

        installException = null;
        installCompleted.Reset();
        hookThread = new Thread(RunHookThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
            Name = "AltGestures低级输入钩子线程"
        };
        hookThread.Start();
        if (!installCompleted.Wait(TimeSpan.FromSeconds(10)))
        {
            // 钩子线程未在超时内报告安装结果：视为安装失败并清理，避免留下已挂载却无法卸载的悬挂钩子。
            var stalled = hookThread;
            hookThread = null;
            if (hookThreadId != 0)
            {
                User32.PostThreadMessage(hookThreadId, NativeConstants.WM_CLOSE, nint.Zero, nint.Zero);
            }
            stalled?.Join(TimeSpan.FromSeconds(2));
            throw new TimeoutException("低级输入钩子安装超时（10 秒），线程未报告安装结果。");
        }

        if (installException is not null)
        {
            hookThread.Join();
            hookThread = null;
            throw installException;
        }
    }

    public void Uninstall()
    {
        var thread = hookThread;
        if (thread is null)
        {
            return;
        }

        if (thread.IsAlive && hookThreadId != 0)
        {
            if (!User32.PostThreadMessage(
                    hookThreadId,
                    NativeConstants.WM_CLOSE,
                    nint.Zero,
                    nint.Zero))
            {
                throw new Win32Exception();
            }
        }

        if (!thread.Join(TimeSpan.FromSeconds(3)))
        {
            throw new TimeoutException("等待钩子线程结束超时。");
        }

        hookThread = null;
        hookThreadId = 0;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Uninstall();
        installCompleted.Dispose();
        disposed = true;
    }

    private void RunHookThread()
    {
        try
        {
            var module = Kernel32.GetModuleHandle(null);
            mouseHook = User32.SetWindowsHookEx(NativeConstants.WH_MOUSE_LL, mouseCallback, module, 0);
            keyboardHook = User32.SetWindowsHookEx(NativeConstants.WH_KEYBOARD_LL, keyboardCallback, module, 0);

            if (mouseHook == nint.Zero || keyboardHook == nint.Zero)
            {
                throw new Win32Exception();
            }

            hookThreadId = Kernel32.GetCurrentThreadId();
            installCompleted.Set();

            while (User32.GetMessage(out var message, nint.Zero, 0, 0) > 0)
            {
                if (message.Message == NativeConstants.WM_CLOSE)
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            installException = exception;
            installCompleted.Set();
        }
        finally
        {
            RemoveHook(ref mouseHook);
            RemoveHook(ref keyboardHook);
            hookThreadId = 0;
        }
    }

    private nint MouseHookProc(int code, nint wParam, nint lParam)
    {
        if (code < 0)
        {
            return User32.CallNextHookEx(mouseHook, code, wParam, lParam);
        }

        var message = MouseMsgMap.FromWindowsMessage((int)wParam);
        if (message is null)
        {
            return User32.CallNextHookEx(mouseHook, code, wParam, lParam);
        }

        var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        User32.GetCursorPos(out var cursor);
        var args = new MouseHookEventArgs(
            message.Value,
            cursor.X,
            cursor.Y,
            data.MouseData,
            data.ExtraInfo);

        try
        {
            MouseHookEvent?.Invoke(args);
        }
        catch (Exception)
        {
            args.Handled = false;
        }

        return args.Handled
            ? new nint(-1)
            : User32.CallNextHookEx(mouseHook, code, wParam, lParam);
    }

    private nint KeyboardHookProc(int code, nint wParam, nint lParam)
    {
        if (code < 0)
        {
            return User32.CallNextHookEx(keyboardHook, code, wParam, lParam);
        }

        var eventType = MouseMsgMap.FromKeyboardWindowsMessage((int)wParam);
        if (eventType is null)
        {
            return User32.CallNextHookEx(keyboardHook, code, wParam, lParam);
        }

        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        var args = new KeyboardHookEventArgs(
            eventType.Value,
            (VirtualKeyCode)data.VirtualKey,
            data.ExtraInfo);

        try
        {
            KeyboardHookEvent?.Invoke(args);
        }
        catch (Exception)
        {
            args.Handled = false;
        }

        return args.Handled
            ? (nint)1
            : User32.CallNextHookEx(keyboardHook, code, wParam, lParam);
    }

    private static void RemoveHook(ref nint hook)
    {
        if (hook != nint.Zero)
        {
            User32.UnhookWindowsHookEx(hook);
            hook = nint.Zero;
        }
    }
}

public sealed class MouseHookEventArgs : EventArgs
{
    public MouseHookEventArgs(
        MouseMsg message,
        int x,
        int y,
        uint mouseData,
        UIntPtr extraInfo)
    {
        Message = message;
        X = x;
        Y = y;
        MouseData = mouseData;
        ExtraInfo = extraInfo;
    }

    public MouseMsg Message { get; }
    public int X { get; }
    public int Y { get; }
    public uint MouseData { get; }
    public UIntPtr ExtraInfo { get; }
    public bool Handled { get; set; }
}

public sealed class KeyboardHookEventArgs : EventArgs
{
    public KeyboardHookEventArgs(
        KeyboardEventType eventType,
        VirtualKeyCode key,
        UIntPtr extraInfo)
    {
        EventType = eventType;
        Key = key;
        ExtraInfo = extraInfo;
    }

    public KeyboardEventType EventType { get; }
    public VirtualKeyCode Key { get; }
    public UIntPtr ExtraInfo { get; }
    public bool Handled { get; set; }
}
