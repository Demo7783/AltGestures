// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Collections.Concurrent;
using System.Drawing;
using AltGestures.Core.Input;
using AltGestures.Core.Interop;

namespace AltGestures.Core.Windows;

public sealed class PathTracker : IPathTracker
{
    private const long SimulatedEventTag = 19900620;
    private const int ScrollModifierIntervalMilliseconds = 100;

    private readonly IMouseKeyboardHook hook;
    private readonly InputSimulator inputSimulator;
    private readonly Func<IReadOnlyList<Rectangle>> screenBoundsFactory;
    private readonly Func<Point, bool, GestureContext> contextFactory;
    private readonly ConcurrentQueue<TrackerMessage> messages = new();
    private readonly AutoResetEvent messageAvailable = new(false);
    private readonly ScreenEdgeInteractDetector edgeDetector;
    private readonly Timer? stayTimer;
    private Thread? workerThread;
    private volatile bool stopRequested;
    private volatile bool paused;
    private volatile bool simulatingInput;
    private volatile bool captured;
    private volatile bool virtualGestureActive;
    private int currentX;
    private int currentY;
    private int moveCount;
    private Point startPoint;
    private Point lastPoint;
    private Point lastEffectivePoint;
    private GestureTriggerButton gestureButton;
    private GestureContext currentContext = new Win32GestureContext();
    private readonly PathEventArgs currentEventArgs = new();
    private long mouseDownTimestamp;
    private long previousModifierTimestamp;
    private GestureModifier filteredModifiers;
    private bool initialMoveValid;
    private bool initialStayTimedOut;
    private bool stayTimedOut;
    private int effectiveMove = 20;
    private int stepSize = 3;
    private bool stayTimeoutEnabled;
    private bool disposed;

    public PathTracker()
        : this(
            new MouseKeyboardHook(),
            new InputSimulator(),
            ScreenInfo.GetAllScreenBounds,
            (point, preferWindowUnderCursor) => Win32GestureContext.Capture(point, preferWindowUnderCursor))
    {
    }

    internal PathTracker(
        IMouseKeyboardHook hook,
        InputSimulator inputSimulator,
        Func<IReadOnlyList<Rectangle>> screenBoundsFactory,
        Func<Point, bool, GestureContext> contextFactory,
        Func<bool>? anyMouseButtonIsDown = null)
    {
        this.hook = hook ?? throw new ArgumentNullException(nameof(hook));
        this.inputSimulator = inputSimulator ?? throw new ArgumentNullException(nameof(inputSimulator));
        this.screenBoundsFactory = screenBoundsFactory ?? throw new ArgumentNullException(nameof(screenBoundsFactory));
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        edgeDetector = new ScreenEdgeInteractDetector(anyMouseButtonIsDown);
        edgeDetector.Rub += edge => Enqueue(MessageKind.EdgeRubbed, parameter: (int)edge);
        edgeDetector.HotCorner += corner => Enqueue(MessageKind.HotCorner, parameter: (int)corner);
        hook.MouseHookEvent += HandleMouseHookEvent;

        stayTimer = new Timer(StayTimeoutCallback);
    }

    public GestureTriggerButton TriggerButton { get; set; } = GestureTriggerButton.Middle;
    public int InitialValidMove { get; set; } = 5;
    public bool InitialStayTimeout { get; set; } = true;
    public int InitialStayTimeoutMillis { get; set; } = 150;
    public bool PreferWindowUnderCursorAsTarget { get; set; }
    public bool DisableInFullscreen { get; set; }
    public int MaxGestureSteps { get; set; } = 4096;
    private bool enableWindowsKeyGesturing;

    public bool EnableWindowsKeyGesturing
    {
        get => enableWindowsKeyGesturing;
        set
        {
            if (value != enableWindowsKeyGesturing)
            {
                if (value)
                {
                    hook.KeyboardHookEvent += HandleKeyboardHookEvent;
                }
                else
                {
                    hook.KeyboardHookEvent -= HandleKeyboardHookEvent;
                }

                enableWindowsKeyGesturing = value;
            }
        }
    }

    public int EffectiveMove
    {
        get => effectiveMove;
        set
        {
            if (value < stepSize)
            {
                throw new ArgumentException("EffectiveMove 不能小于 StepSize。");
            }

            effectiveMove = value;
        }
    }

    public int StepSize
    {
        get => stepSize;
        set
        {
            if (value > EffectiveMove)
            {
                throw new ArgumentException("StepSize 不能大于 EffectiveMove。");
            }

            stepSize = value;
        }
    }

    public bool StayTimeout
    {
        get => stayTimeoutEnabled;
        set
        {
            stayTimeoutEnabled = value;
            if (!value)
            {
                stayTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    public int StayTimeoutMillis { get; set; } = 500;
    public bool PerformNormalWhenTimeout { get; set; }
    public bool IsStarted => !stopRequested && workerThread is { IsAlive: true };
    public bool IsDisposed => disposed;

    public bool Paused
    {
        get => paused;
        set
        {
            paused = value;
            edgeDetector.Paused = value;
        }
    }

    public bool IsSuspended { get; private set; }

    public event BeforePathStartEventHandler? BeforePathStart;
    public event PathTrackEventHandler? PathStart;
    public event PathTrackEventHandler? PathGrow;
    public event PathTrackEventHandler? EffectivePathGrow;
    public event PathTrackEventHandler? PathEnd;
    public event PathTrackEventHandler? PathTimeout;
    public event PathTrackEventHandler? PathModifier;
    public event Action<ScreenCorner>? HotCornerTriggered;
    public event Action<ScreenEdge>? EdgeRubbed;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (IsStarted)
        {
            throw new InvalidOperationException("路径追踪器已经启动。");
        }

        stopRequested = false;
        edgeDetector.SetScreens(screenBoundsFactory(), ScreenInfo.GetDpiScale());
        workerThread = new Thread(ProcessMessages)
        {
            IsBackground = true,
            Name = "AltGestures路径追踪工作者"
        };
        workerThread.Start();

        try
        {
            hook.Install();
        }
        catch
        {
            StopWorker();
            throw;
        }
    }

    public void Stop()
    {
        if (disposed || stopRequested)
        {
            return;
        }

        stopRequested = true;
        Enqueue(MessageKind.Stop);
        StopWorker();
        hook.Uninstall();
        paused = false;
        edgeDetector.Paused = false;
    }

    public void SuspendTemprarily(GestureModifier filteredModifiers)
    {
        this.filteredModifiers = filteredModifiers;
        IsSuspended = true;
        stayTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void ResumeSuspension()
    {
        filteredModifiers = GestureModifier.None;
        IsSuspended = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (!stopRequested)
        {
            stopRequested = true;
            Enqueue(MessageKind.Stop);
            StopWorker();
        }

        hook.MouseHookEvent -= HandleMouseHookEvent;
        hook.KeyboardHookEvent -= HandleKeyboardHookEvent;
        hook.Dispose();
        edgeDetector.Dispose();
        stayTimer?.Dispose();
        messageAvailable.Dispose();
    }

    internal static bool ShouldRecordStep(int currentStep, int maximumSteps) =>
        currentStep >= 0 && maximumSteps > 0 && currentStep < maximumSteps;

    internal static bool HasEffectiveMove(Point lastPoint, Point currentPoint, int threshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        return GetDistance(lastPoint, currentPoint) > threshold;
    }

    internal static GestureTriggerButton ToGestureTriggerButton(
        MouseMsg message,
        XButtonNumber xButtonNumber) => message switch
    {
        MouseMsg.WMRButtonDown => GestureTriggerButton.Right,
        MouseMsg.WMMButtonDown => GestureTriggerButton.Middle,
        MouseMsg.WMXButtonDown => xButtonNumber == XButtonNumber.One
            ? GestureTriggerButton.X1
            : GestureTriggerButton.X2,
        _ => throw new ArgumentOutOfRangeException(nameof(message), message, "不是手势触发键按下消息。")
    };

    internal static GestureModifier ToGestureModifier(
        MouseMsg message,
        XButtonNumber xButtonNumber) => message switch
    {
        MouseMsg.WMLButtonDown => GestureModifier.LeftButtonDown,
        MouseMsg.WMRButtonDown => GestureModifier.RightButtonDown,
        MouseMsg.WMMButtonDown => GestureModifier.MiddleButtonDown,
        MouseMsg.WMXButtonDown => xButtonNumber == XButtonNumber.One
            ? GestureModifier.X1
            : GestureModifier.X2,
        _ => throw new ArgumentOutOfRangeException(nameof(message), message, "不是鼠标修饰键按下消息。")
    };

    internal void ProcessMouseDown(Point point)
    {
        currentX = point.X;
        currentY = point.Y;
        startPoint = point;
        lastPoint = point;
        lastEffectivePoint = point;
        moveCount = 0;
        stayTimedOut = false;
        initialStayTimedOut = false;
        initialMoveValid = false;
        mouseDownTimestamp = Environment.TickCount64;

        var screenBounds = ScreenBounds.FromPoint(point, screenBoundsFactory());
        EffectiveMove = Math.Max(StepSize, (int)(screenBounds.Width * 0.025f));

        try
        {
            currentContext = contextFactory(point, PreferWindowUnderCursorAsTarget);
            UpdateEventArgs(point);
            var args = new BeforePathStartEventArgs(currentEventArgs);
            BeforePathStart?.Invoke(args);
            if (!args.ShouldPathStart)
            {
                captured = false;
                ReplayGestureButtonClick();
            }
        }
        catch (Exception)
        {
            captured = false;
            ReplayGestureButtonClick();
        }
    }

    internal void ProcessMouseMove(Point point)
    {
        currentX = point.X;
        currentY = point.Y;
        if (!captured || (StayTimeout && stayTimedOut) || IsSuspended)
        {
            return;
        }

        if (!initialMoveValid)
        {
            if (InitialStayTimeout && !initialStayTimedOut)
            {
                var now = Environment.TickCount64;
                if (now - mouseDownTimestamp > InitialStayTimeoutMillis)
                {
                    initialStayTimedOut = true;
                    return;
                }

                mouseDownTimestamp = now;
            }

            if (GetDistance(point, startPoint) <= InitialValidMove)
            {
                RestartStayTimer();
                return;
            }

            initialMoveValid = true;
            UpdateEventArgs(startPoint);
            PathStart?.Invoke(currentEventArgs);
        }

        if (InitialStayTimeout && initialStayTimedOut)
        {
            return;
        }

        if (!ShouldRecordStep(moveCount, MaxGestureSteps))
        {
            return;
        }

        moveCount++;
        UpdateEventArgs(point);
        if (GetDistance(point, lastPoint) >= StepSize)
        {
            PathGrow?.Invoke(currentEventArgs);
            lastPoint = point;
        }

        if (HasEffectiveMove(lastEffectivePoint, point, EffectiveMove))
        {
            EffectivePathGrow?.Invoke(currentEventArgs);
            lastEffectivePoint = point;
        }

        RestartStayTimer();
    }

    internal void ProcessMouseUp(Point point)
    {
        currentX = point.X;
        currentY = point.Y;
        if (!captured)
        {
            return;
        }

        if (!initialMoveValid)
        {
            captured = false;
            ReplayGestureButtonClick();
            ResetPathState();
            return;
        }

        stayTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        if (!stayTimedOut)
        {
            UpdateEventArgs(point);
            PathEnd?.Invoke(currentEventArgs);
        }

        ResetPathState();
    }

    internal void ProcessModifier(GestureModifier modifier)
    {
        if (!captured || stayTimedOut || IsModifierFiltered(modifier))
        {
            return;
        }

        if (!initialMoveValid)
        {
            initialMoveValid = true;
            UpdateEventArgs(startPoint);
            PathStart?.Invoke(currentEventArgs);
        }

        currentEventArgs.Modifier = modifier;
        PathModifier?.Invoke(currentEventArgs);
        RestartStayTimer();
        previousModifierTimestamp = Environment.TickCount64;
    }

    internal void ProcessHotCorner(ScreenCorner corner)
    {
        if (!DisableInFullscreen || !IsInFullScreenMode())
        {
            HotCornerTriggered?.Invoke(corner);
        }
    }

    internal void ProcessEdgeRubbed(ScreenEdge edge)
    {
        if (!DisableInFullscreen || !IsInFullScreenMode())
        {
            EdgeRubbed?.Invoke(edge);
        }
    }

    private void HandleMouseHookEvent(MouseHookEventArgs args)
    {
        if (paused || simulatingInput || args.ExtraInfo == (UIntPtr)SimulatedEventTag)
        {
            return;
        }

        currentX = args.X;
        currentY = args.Y;
        edgeDetector.HandleMouse(args);

        switch (args.Message)
        {
            case MouseMsg.WMMouseMove:
                if (captured)
                {
                    Enqueue(MessageKind.MouseMove, x: args.X, y: args.Y);
                }

                break;

            case MouseMsg.WMRButtonDown:
            case MouseMsg.WMMButtonDown:
            case MouseMsg.WMXButtonDown:
                if (captured)
                {
                    var xButton = args.Message == MouseMsg.WMXButtonDown
                        ? MouseMsgMap.FromNativeMouseData(args.MouseData)
                        : XButtonNumber.One;
                    args.Handled = QueueModifier(ToGestureModifier(args.Message, xButton));
                }
                else if (IsTriggerButton(args.Message, args.MouseData))
                {
                    captured = true;
                    gestureButton = ToGestureTriggerButton(
                        args.Message,
                        args.Message == MouseMsg.WMXButtonDown
                            ? MouseMsgMap.FromNativeMouseData(args.MouseData)
                            : XButtonNumber.One);
                    args.Handled = true;
                    Enqueue(MessageKind.MouseDown, x: args.X, y: args.Y);
                }

                break;

            case MouseMsg.WMMouseWheel:
                if (captured)
                {
                    var delta = (short)(args.MouseData >> 16);
                    var modifier = delta > 0
                        ? GestureModifier.WheelForward
                        : GestureModifier.WheelBackward;
                    args.Handled = QueueModifier(modifier);
                }
                else if (Environment.TickCount64 - previousModifierTimestamp < 300)
                {
                    args.Handled = true;
                }

                break;

            case MouseMsg.WMLButtonDown:
                if (captured)
                {
                    args.Handled = QueueModifier(GestureModifier.LeftButtonDown);
                }

                break;

            case MouseMsg.WMRButtonUp:
            case MouseMsg.WMMButtonUp:
            case MouseMsg.WMXButtonUp:
                if (captured)
                {
                    args.Handled = true;
                    if (IsGestureButtonUp(args.Message))
                    {
                        captured = false;
                        Enqueue(MessageKind.MouseUp, x: args.X, y: args.Y);
                    }
                }

                break;
        }
    }

    private void HandleKeyboardHookEvent(KeyboardHookEventArgs args)
    {
        if (paused || simulatingInput || args.ExtraInfo == (UIntPtr)SimulatedEventTag)
        {
            return;
        }

        if (!EnableWindowsKeyGesturing)
        {
            return;
        }

        if (args.Key != VirtualKeyCode.VK_LWIN)
        {
            if (!virtualGestureActive || args.EventType != KeyboardEventType.KeyDown)
            {
                return;
            }

            virtualGestureActive = false;
            args.Handled = true;
            Enqueue(MessageKind.ReplayWindowsCombination, parameter: (int)args.Key);
            return;
        }

        if (args.EventType == KeyboardEventType.KeyDown)
        {
            if (!captured && !virtualGestureActive)
            {
                virtualGestureActive = true;
                args.Handled = true;
            }

            return;
        }

        if (!virtualGestureActive)
        {
            return;
        }

        virtualGestureActive = false;
        args.Handled = true;
        if (captured)
        {
            captured = false;
            Enqueue(MessageKind.MouseUp, x: currentX, y: currentY, parameter: 1);
        }
        else
        {
            Enqueue(MessageKind.ReplayWindowsCombination, parameter: (int)args.Key);
        }
    }

    private bool QueueModifier(GestureModifier modifier)
    {
        if (IsModifierFiltered(modifier))
        {
            return false;
        }

        if ((modifier & GestureModifier.Scroll) == modifier)
        {
            var now = Environment.TickCount64;
            if (now - previousModifierTimestamp <= ScrollModifierIntervalMilliseconds)
            {
                return true;
            }
        }

        Enqueue(MessageKind.Modifier, parameter: (int)modifier);
        return true;
    }

    private void ProcessMessages()
    {
        while (!stopRequested)
        {
            if (messages.TryDequeue(out var message))
            {
                ProcessMessage(message);
                continue;
            }

            _ = messageAvailable.WaitOne(50);
        }

        while (messages.TryDequeue(out var message) && message.Kind != MessageKind.Stop)
        {
        }
    }

    private void ProcessMessage(TrackerMessage message)
    {
        switch (message.Kind)
        {
            case MessageKind.MouseDown:
                ProcessMouseDown(new Point(message.X, message.Y));
                break;
            case MessageKind.MouseMove:
                ProcessMouseMove(new Point(message.X, message.Y));
                break;
            case MessageKind.MouseUp:
                ProcessMouseUp(new Point(message.X, message.Y));
                break;
            case MessageKind.Modifier:
                ProcessModifier((GestureModifier)message.Parameter);
                break;
            case MessageKind.HotCorner:
                ProcessHotCorner((ScreenCorner)message.Parameter);
                break;
            case MessageKind.EdgeRubbed:
                ProcessEdgeRubbed((ScreenEdge)message.Parameter);
                break;
            case MessageKind.Timeout:
                ProcessTimeout();
                break;
            case MessageKind.ReplayWindowsCombination:
                ReplayWindowsCombination((VirtualKeyCode)message.Parameter);
                break;
            case MessageKind.Stop:
                stopRequested = true;
                break;
            default:
                throw new InvalidOperationException($"未知路径追踪消息：{message.Kind}。");
        }
    }

    private void ProcessTimeout()
    {
        if (!captured || !initialMoveValid)
        {
            return;
        }

        stayTimedOut = true;
        if (PerformNormalWhenTimeout)
        {
            ReplayGestureButton(replayDown: false);
        }

        UpdateEventArgs(new Point(currentX, currentY));
        PathTimeout?.Invoke(currentEventArgs);
    }

    private void StayTimeoutCallback(object? state)
    {
        if (captured && initialMoveValid && !IsSuspended)
        {
            Enqueue(MessageKind.Timeout);
        }
    }

    private void RestartStayTimer()
    {
        if (StayTimeout && initialMoveValid && !initialStayTimedOut && !IsSuspended)
        {
            stayTimer?.Change(StayTimeoutMillis, Timeout.Infinite);
        }
    }

    private void UpdateEventArgs(Point point)
    {
        currentContext.StartPoint = initialMoveValid ? startPoint : point;
        currentContext.EndPoint = point;
        currentContext.GestureButton = gestureButton;
        currentEventArgs.Context = currentContext;
        currentEventArgs.Location = point;
        currentEventArgs.Button = gestureButton;
        currentEventArgs.Modifier = GestureModifier.None;
    }

    private void ResetPathState()
    {
        captured = false;
        IsSuspended = false;
        filteredModifiers = GestureModifier.None;
        moveCount = 0;
    }

    private bool IsTriggerButton(MouseMsg message, uint mouseData)
    {
        if (message == MouseMsg.WMRButtonDown)
        {
            return (TriggerButton & GestureTriggerButton.Right) != 0;
        }

        if (message == MouseMsg.WMMButtonDown)
        {
            return (TriggerButton & GestureTriggerButton.Middle) != 0;
        }

        if (message != MouseMsg.WMXButtonDown)
        {
            return false;
        }

        return MouseMsgMap.FromNativeMouseData(mouseData) switch
        {
            XButtonNumber.One => (TriggerButton & GestureTriggerButton.X1) != 0,
            XButtonNumber.Two => (TriggerButton & GestureTriggerButton.X2) != 0,
            _ => false
        };
    }

    private bool IsGestureButtonUp(MouseMsg message) => gestureButton switch
    {
        GestureTriggerButton.Right => message == MouseMsg.WMRButtonUp,
        GestureTriggerButton.Middle => message == MouseMsg.WMMButtonUp,
        GestureTriggerButton.X1 or GestureTriggerButton.X2 => message == MouseMsg.WMXButtonUp,
        _ => false
    };

    private bool IsModifierFiltered(GestureModifier modifier) =>
        modifier != GestureModifier.None && (modifier & filteredModifiers) == modifier;

    private bool IsInFullScreenMode()
    {
        var foregroundWindow = User32.GetAncestor(User32.GetForegroundWindow(), NativeConstants.GA_ROOT);
        var desktopWindow = User32.GetDesktopWindow();
        var shellWindow = User32.GetShellWindow();
        if (foregroundWindow == nint.Zero ||
            foregroundWindow == desktopWindow ||
            foregroundWindow == shellWindow ||
            !User32.GetWindowRect(foregroundWindow, out var windowRect))
        {
            return false;
        }

        return ScreenBounds.FromPoint(new Point(currentX, currentY), screenBoundsFactory()) == windowRect;
    }

    private void ReplayGestureButtonClick()
    {
        simulatingInput = true;
        inputSimulator.ExtraInfo = (nint)SimulatedEventTag;
        try
        {
            ReplayGestureButton(replayDown: true);
        }
        finally
        {
            inputSimulator.ExtraInfo = 0;
            simulatingInput = false;
        }
    }

    private void ReplayGestureButton(bool replayDown)
    {
        switch (gestureButton)
        {
            case GestureTriggerButton.Right:
                ReplayMouseButton(inputSimulator.RightButtonDown, inputSimulator.RightButtonUp, replayDown);
                break;
            case GestureTriggerButton.Middle:
                ReplayMouseButton(inputSimulator.MiddleButtonDown, inputSimulator.MiddleButtonUp, replayDown);
                break;
            case GestureTriggerButton.X1:
                ReplayXButton(VirtualKeyCode.VK_XBUTTON1, replayDown);
                break;
            case GestureTriggerButton.X2:
                ReplayXButton(VirtualKeyCode.VK_XBUTTON2, replayDown);
                break;
        }
    }

    private static void ReplayMouseButton(Func<bool> down, Func<bool> up, bool replayDown)
    {
        if (replayDown)
        {
            down();
        }

        up();
    }

    private void ReplayXButton(VirtualKeyCode virtualKey, bool replayDown)
    {
        if (replayDown)
        {
            _ = inputSimulator.XButtonDown(virtualKey);
        }

        _ = inputSimulator.XButtonUp(virtualKey);
    }

    private void ReplayWindowsCombination(VirtualKeyCode key)
    {
        simulatingInput = true;
        inputSimulator.ExtraInfo = (nint)SimulatedEventTag;
        try
        {
            if (key == VirtualKeyCode.VK_LWIN)
            {
                _ = inputSimulator.Press(key);
            }
            else
            {
                _ = inputSimulator.Press(VirtualKeyCode.VK_LWIN, key);
            }
        }
        finally
        {
            inputSimulator.ExtraInfo = 0;
            simulatingInput = false;
        }
    }

    private void Enqueue(MessageKind kind, int x = 0, int y = 0, int parameter = 0)
    {
        messages.Enqueue(new TrackerMessage(kind, x, y, parameter));
        _ = messageAvailable.Set();
    }

    private void StopWorker()
    {
        _ = messageAvailable.Set();
        workerThread?.Join(TimeSpan.FromSeconds(3));
        workerThread = null;
    }

    private static int GetDistance(Point first, Point second)
    {
        var deltaX = first.X - second.X;
        var deltaY = first.Y - second.Y;
        return (int)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private readonly record struct ScreenBounds(int X, int Y, int Width, int Height)
    {
        public static ScreenBounds FromPoint(Point point, IEnumerable<Rectangle> screens)
        {
            foreach (var screen in screens)
            {
                if (screen.Contains(point))
                {
                    return new ScreenBounds(screen.X, screen.Y, screen.Width, screen.Height);
                }
            }

            return new ScreenBounds(0, 0, 1920, 1080);
        }

        public static implicit operator ScreenBounds(RECT rect) => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    private enum MessageKind
    {
        Stop,
        MouseDown,
        MouseMove,
        MouseUp,
        Modifier,
        HotCorner,
        EdgeRubbed,
        Timeout,
        ReplayWindowsCombination
    }

    private readonly record struct TrackerMessage(MessageKind Kind, int X, int Y, int Parameter);
}

internal interface IMouseKeyboardHook : IDisposable
{
    event Action<MouseHookEventArgs>? MouseHookEvent;
    event Action<KeyboardHookEventArgs>? KeyboardHookEvent;

    bool IsInstalled { get; }

    void Install();

    void Uninstall();
}
