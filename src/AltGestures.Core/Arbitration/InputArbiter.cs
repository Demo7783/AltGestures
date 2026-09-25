using System.Collections.Concurrent;
using AltGestures.Core.Input;
using AltGestures.Core.Interop;
using AltGestures.Core.Windowing;
using AltGestures.Core.Windows;

namespace AltGestures.Core.Arbitration;

/// <summary>
/// 在低级输入钩子、窗口分支与手势分支之间做统一仲裁。
/// </summary>
public sealed class InputArbiter : IMouseKeyboardHook
{
    private readonly IMouseKeyboardHook upstream;
    private readonly IWindowBranch windowBranch;
    private readonly WindowActionBinding bindings;
    private readonly Func<ModifierModifiers> modifierState;
    private readonly Func<long> timestamp;
    private readonly Func<uint> doubleClickTime;
    private readonly ConcurrentQueue<ArbiterMessage> messages = new();
    private readonly AutoResetEvent messageAvailable = new(false);
    private readonly bool dispatchSynchronously;
    private Thread? workerThread;
    private volatile bool stopRequested;
    private bool disposed;
    private WindowMouseButton? capturedButton;
    private readonly long[] lastClickTimestamps = [-1_000_000_000, -1_000_000_000, -1_000_000_000, -1_000_000_000, -1_000_000_000];
    private ModifierModifiers modifiers;
    private bool spaceDown;

    public InputArbiter(
        MouseKeyboardHook upstream,
        IWindowBranch windowBranch,
        WindowActionBinding bindings,
        Func<long>? timestamp = null,
        Func<uint>? doubleClickTime = null)
        : this(
            upstream,
            windowBranch,
            bindings,
            () => new ModifierState().Current,
            timestamp,
            doubleClickTime,
            dispatchSynchronously: false)
    {
    }

    internal InputArbiter(
        IMouseKeyboardHook upstream,
        IWindowBranch windowBranch,
        WindowActionBinding bindings,
        Func<ModifierModifiers>? modifierState = null,
        Func<long>? timestamp = null,
        Func<uint>? doubleClickTime = null,
        bool dispatchSynchronously = false)
    {
        this.upstream = upstream ?? throw new ArgumentNullException(nameof(upstream));
        this.windowBranch = windowBranch ?? throw new ArgumentNullException(nameof(windowBranch));
        this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        this.modifierState = modifierState ?? (() => new ModifierState().Current);
        this.timestamp = timestamp ?? (() => Environment.TickCount64);
        this.doubleClickTime = doubleClickTime ?? User32.GetDoubleClickTime;
        this.dispatchSynchronously = dispatchSynchronously;

        upstream.MouseHookEvent += HandleUpstreamMouse;
        upstream.KeyboardHookEvent += HandleUpstreamKeyboard;
    }

    public event Action<MouseHookEventArgs>? MouseHookEvent;

    public event Action<KeyboardHookEventArgs>? KeyboardHookEvent;

    public InputOwnership Ownership { get; private set; }

    public ArbitrationState State { get; private set; }

    public bool IsInstalled => upstream.IsInstalled;

    public void Install()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        StartWorker();
        upstream.Install();
    }

    public void Uninstall()
    {
        upstream.Uninstall();
        StopWorker();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        upstream.MouseHookEvent -= HandleUpstreamMouse;
        upstream.KeyboardHookEvent -= HandleUpstreamKeyboard;
        upstream.Dispose();
        StopWorker();
        messageAvailable.Dispose();
        disposed = true;
    }

    private void HandleUpstreamMouse(MouseHookEventArgs args)
    {
        var handled = args.Message switch
        {
            MouseMsg.WMLButtonDown => ProcessMouseDown(WindowMouseButton.Left, args),
            MouseMsg.WMMButtonDown => ProcessMouseDown(WindowMouseButton.Middle, args),
            MouseMsg.WMRButtonDown => ProcessMouseDown(WindowMouseButton.Right, args),
            MouseMsg.WMXButtonDown => ProcessXMouseDown(args),
            MouseMsg.WMLButtonUp => ProcessMouseUp(WindowMouseButton.Left, args),
            MouseMsg.WMMButtonUp => ProcessMouseUp(WindowMouseButton.Middle, args),
            MouseMsg.WMRButtonUp => ProcessMouseUp(WindowMouseButton.Right, args),
            MouseMsg.WMXButtonUp => ProcessXMouseUp(args),
            MouseMsg.WMMouseMove => ProcessMouseMove(args),
            MouseMsg.WMMouseWheel => ProcessMouseWheel(args),
            _ => false
        };

        if (handled)
        {
            args.Handled = true;
            return;
        }

        MouseHookEvent?.Invoke(args);
    }

    private bool ProcessMouseDown(WindowMouseButton button, MouseHookEventArgs args)
    {
        if (capturedButton is not null)
        {
            return Ownership == InputOwnership.Window;
        }

        modifiers = modifierState();
        var action = bindings[ToTrigger(button)];
        if (!IsAltDown(modifiers) || action == WindowAction.None)
        {
            Ownership = InputOwnership.Gesture;
            State = ArbitrationState.GestureTracking;
            capturedButton = button;
            return false;
        }

        var now = timestamp();
        var isDoubleClick = IsDoubleClick(button, now);
        lastClickTimestamps[(int)button] = now;

        Ownership = InputOwnership.Window;
        State = action is WindowAction.Move or WindowAction.Resize && !isDoubleClick
            ? ArbitrationState.WindowDragging
            : ArbitrationState.WindowCommand;
        capturedButton = button;
        Dispatch(ArbiterMessage.MouseDown(
            action,
            button,
            args.X,
            args.Y,
            ToDragModifiers(modifiers, spaceDown),
            isDoubleClick));
        return true;
    }

    private bool ProcessXMouseDown(MouseHookEventArgs args) =>
        MouseMsgMap.FromNativeMouseData(args.MouseData) == XButtonNumber.One
            ? ProcessMouseDown(WindowMouseButton.X1, args)
            : ProcessMouseDown(WindowMouseButton.X2, args);

    private bool ProcessXMouseUp(MouseHookEventArgs args) =>
        MouseMsgMap.FromNativeMouseData(args.MouseData) == XButtonNumber.One
            ? ProcessMouseUp(WindowMouseButton.X1, args)
            : ProcessMouseUp(WindowMouseButton.X2, args);

    private bool ProcessMouseMove(MouseHookEventArgs args)
    {
        if (capturedButton is null)
        {
            return false;
        }

        if (Ownership == InputOwnership.Window)
        {
            modifiers = modifierState();
            Dispatch(ArbiterMessage.MouseMove(args.X, args.Y, ToDragModifiers(modifiers, spaceDown)));
            return true;
        }

        return false;
    }

    private bool ProcessMouseWheel(MouseHookEventArgs args)
    {
        if (Ownership == InputOwnership.Window)
        {
            return true;
        }

        var delta = (short)(args.MouseData >> 16);
        modifiers = modifierState();
        var action = bindings.GetWheelAction(delta);
        if (!IsAltDown(modifiers) || action == WindowAction.None)
        {
            return false;
        }

        State = ArbitrationState.WindowCommand;
        Dispatch(ArbiterMessage.MouseWheel(action, delta, args.X, args.Y, ToDragModifiers(modifiers, spaceDown)));
        return true;
    }

    private bool ProcessMouseUp(WindowMouseButton button, MouseHookEventArgs args)
    {
        if (capturedButton != button)
        {
            return false;
        }

        if (Ownership == InputOwnership.Window)
        {
            Dispatch(ArbiterMessage.MouseUp(button, args.X, args.Y));
            capturedButton = null;
            Ownership = InputOwnership.None;
            State = ArbitrationState.Idle;
            return true;
        }

        capturedButton = null;
        Ownership = InputOwnership.None;
        State = ArbitrationState.Idle;
        return false;
    }

    private void HandleUpstreamKeyboard(KeyboardHookEventArgs args)
    {
        UpdateModifiers(args);
        if (Ownership == InputOwnership.Window)
        {
            args.Handled = true;
            return;
        }

        KeyboardHookEvent?.Invoke(args);
    }

    private void UpdateModifiers(KeyboardHookEventArgs args)
    {
        var isDown = args.EventType == KeyboardEventType.KeyDown;
        if (args.Key == VirtualKeyCode.VK_SPACE)
        {
            spaceDown = isDown;
        }

        modifiers = args.Key switch
        {
            VirtualKeyCode.VK_LSHIFT => WithModifier(modifiers, ModifierModifiers.LeftShift, isDown),
            VirtualKeyCode.VK_RSHIFT => WithModifier(modifiers, ModifierModifiers.RightShift, isDown),
            VirtualKeyCode.VK_LCONTROL => WithModifier(modifiers, ModifierModifiers.LeftControl, isDown),
            VirtualKeyCode.VK_RCONTROL => WithModifier(modifiers, ModifierModifiers.RightControl, isDown),
            VirtualKeyCode.VK_LMENU => WithModifier(modifiers, ModifierModifiers.LeftAlt, isDown),
            VirtualKeyCode.VK_RMENU => WithModifier(modifiers, ModifierModifiers.RightAlt, isDown),
            _ => modifiers
        };
    }

    private void Dispatch(ArbiterMessage message)
    {
        if (dispatchSynchronously)
        {
            DispatchMessage(message);
            return;
        }

        messages.Enqueue(message);
        _ = messageAvailable.Set();
    }

    private void StartWorker()
    {
        if (workerThread is not null || dispatchSynchronously)
        {
            return;
        }

        stopRequested = false;
        workerThread = new Thread(ProcessMessages)
        {
            IsBackground = true,
            Name = "AltGestures窗口操作工作者"
        };
        workerThread.Start();
    }

    private void StopWorker()
    {
        var thread = workerThread;
        workerThread = null;
        if (thread is null)
        {
            return;
        }

        stopRequested = true;
        _ = messageAvailable.Set();
        thread.Join(TimeSpan.FromSeconds(3));
    }

    private void ProcessMessages()
    {
        while (!stopRequested)
        {
            if (messages.TryDequeue(out var message))
            {
                DispatchMessage(message);
                continue;
            }

            messageAvailable.WaitOne(TimeSpan.FromMilliseconds(100));
        }
    }

    private void DispatchMessage(ArbiterMessage message)
    {
        switch (message.Kind)
        {
            case ArbiterMessageKind.MouseDown:
                windowBranch.OnMouseDown(
                    message.Action,
                    message.Button,
                    message.X,
                    message.Y,
                    message.Modifiers,
                    message.IsDoubleClick);
                break;
            case ArbiterMessageKind.MouseMove:
                windowBranch.OnMouseMove(message.X, message.Y, message.Modifiers);
                break;
            case ArbiterMessageKind.MouseUp:
                windowBranch.OnMouseUp(message.Button, message.X, message.Y);
                break;
            case ArbiterMessageKind.MouseWheel:
                windowBranch.OnMouseWheel(message.Delta, message.X, message.Y, message.Modifiers);
                break;
        }
    }

    private bool IsDoubleClick(WindowMouseButton button, long now)
    {
        if (button is not (WindowMouseButton.Left or WindowMouseButton.Right))
        {
            return false;
        }

        var elapsed = now - lastClickTimestamps[(int)button];
        return elapsed >= 0 && elapsed < doubleClickTime();
    }

    private static WindowTrigger ToTrigger(WindowMouseButton button) => button switch
    {
        WindowMouseButton.Left => WindowTrigger.Left,
        WindowMouseButton.Middle => WindowTrigger.Middle,
        WindowMouseButton.Right => WindowTrigger.Right,
        WindowMouseButton.X1 => WindowTrigger.X1,
        WindowMouseButton.X2 => WindowTrigger.X2,
        _ => throw new ArgumentOutOfRangeException(nameof(button))
    };

    private static WindowDragModifiers ToDragModifiers(ModifierModifiers value, bool spaceDown) =>
        (value.HasFlag(ModifierModifiers.LeftShift) || value.HasFlag(ModifierModifiers.RightShift)
            ? WindowDragModifiers.Shift
            : WindowDragModifiers.None)
        | (value.HasFlag(ModifierModifiers.LeftControl) || value.HasFlag(ModifierModifiers.RightControl)
            ? WindowDragModifiers.Control
            : WindowDragModifiers.None)
        | (spaceDown ? WindowDragModifiers.Space : WindowDragModifiers.None);

    private static ModifierModifiers WithModifier(
        ModifierModifiers current,
        ModifierModifiers modifier,
        bool isDown) => isDown ? current | modifier : current & ~modifier;

    private readonly record struct ArbiterMessage(
        ArbiterMessageKind Kind,
        WindowAction Action,
        WindowMouseButton Button,
        int X,
        int Y,
        int Delta,
        WindowDragModifiers Modifiers,
        bool IsDoubleClick)
    {
        public static ArbiterMessage MouseDown(
            WindowAction action,
            WindowMouseButton button,
            int x,
            int y,
            WindowDragModifiers modifiers,
            bool isDoubleClick) => new(
                ArbiterMessageKind.MouseDown,
                action,
                button,
                x,
                y,
            0,
            modifiers,
            isDoubleClick);

        public static ArbiterMessage MouseMove(int x, int y, WindowDragModifiers modifiers) => new(
            ArbiterMessageKind.MouseMove,
            WindowAction.None,
            WindowMouseButton.Left,
            x,
            y,
            0,
            modifiers,
            false);

        public static ArbiterMessage MouseUp(WindowMouseButton button, int x, int y) => new(
            ArbiterMessageKind.MouseUp,
            WindowAction.None,
            button,
            x,
            y,
            0,
            WindowDragModifiers.None,
            false);

        public static ArbiterMessage MouseWheel(
            WindowAction action,
            int delta,
            int x,
            int y,
            WindowDragModifiers modifiers) => new(
            ArbiterMessageKind.MouseWheel,
            action,
            WindowMouseButton.Left,
            x,
            y,
            delta,
            modifiers,
            false);

    }

    private static bool IsAltDown(ModifierModifiers value) =>
        (value & (ModifierModifiers.LeftAlt | ModifierModifiers.RightAlt)) != 0;

    private enum ArbiterMessageKind
    {
        MouseDown,
        MouseMove,
        MouseUp,
        MouseWheel
    }
}
