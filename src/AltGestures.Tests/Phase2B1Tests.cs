using System.Drawing;
using AltGestures.Core;
using AltGestures.Core.Input;
using AltGestures.Core.Interop;
using AltGestures.Core.Windows;
using Xunit;

namespace AltGestures.Tests;

public sealed class Phase2B1Tests
{
    [Theory]
    [InlineData(NativeConstants.WM_MOUSEMOVE, MouseMsg.WMMouseMove)]
    [InlineData(NativeConstants.WM_LBUTTONDOWN, MouseMsg.WMLButtonDown)]
    [InlineData(NativeConstants.WM_RBUTTONDOWN, MouseMsg.WMRButtonDown)]
    [InlineData(NativeConstants.WM_MBUTTONDOWN, MouseMsg.WMMButtonDown)]
    [InlineData(NativeConstants.WM_XBUTTONDOWN, MouseMsg.WMXButtonDown)]
    [InlineData(NativeConstants.WM_XBUTTONUP, MouseMsg.WMXButtonUp)]
    public void MouseMessagesMatchWindowsConstants(int message, MouseMsg expected)
    {
        Assert.Equal(expected, MouseMsgMap.FromWindowsMessage(message));
        Assert.Equal(message, MouseMsgMap.ToWindowsMessage(expected));
    }

    [Fact]
    public void XButtonNumbersUseNativeDataValues()
    {
        Assert.Equal(XButtonNumber.One, MouseMsgMap.FromNativeMouseData(1u << 16));
        Assert.Equal(XButtonNumber.Two, MouseMsgMap.FromNativeMouseData(2u << 16));
    }

    [Theory]
    [InlineData(0, 500, ScreenEdge.Left)]
    [InlineData(1910, 500, ScreenEdge.Right)]
    [InlineData(500, 0, ScreenEdge.Top)]
    [InlineData(500, 1070, ScreenEdge.Bottom)]
    public void DetectsScreenEdges(int x, int y, ScreenEdge expected)
    {
        Assert.Equal(expected, ScreenEdgeInteractDetector.GetEdge(new Point(x, y), new Size(1920, 1080)));
    }

    [Fact]
    public void CenterPointIsNotAScreenEdge()
    {
        Assert.Null(ScreenEdgeInteractDetector.GetEdge(new Point(960, 540), new Size(1920, 1080)));
    }

    [Theory]
    [InlineData(0, 1080, ScreenCorner.LeftBottom)]
    [InlineData(0, 0, ScreenCorner.LeftTop)]
    [InlineData(1920, 0, ScreenCorner.RightTop)]
    [InlineData(1920, 1080, ScreenCorner.RightBottom)]
    public void DetectsHotCorners(int x, int y, ScreenCorner expected)
    {
        Assert.Equal(
            expected,
            ScreenEdgeInteractDetector.GetCorner(
                new Point(x, y),
                new Rectangle(0, 0, 1920, 1080)));
    }

    [Fact]
    public void StepsAfterMaximumAreIgnored()
    {
        Assert.True(PathTracker.ShouldRecordStep(0, 2));
        Assert.True(PathTracker.ShouldRecordStep(1, 2));
        Assert.False(PathTracker.ShouldRecordStep(2, 2));
        Assert.False(PathTracker.ShouldRecordStep(3, 2));
    }

    [Fact]
    public void SmallJitterIsNotEffectiveMovement()
    {
        Assert.False(PathTracker.HasEffectiveMove(new Point(100, 100), new Point(103, 100), 5));
        Assert.True(PathTracker.HasEffectiveMove(new Point(100, 100), new Point(106, 100), 5));
    }

    [Fact]
    public void PathTrackerLifecycleTransitionsWithoutWin32Calls()
    {
        using var hook = new FakeMouseKeyboardHook();
        using var tracker = CreateTracker(hook);

        tracker.Start();
        Assert.True(hook.IsInstalled);
        Assert.True(tracker.IsStarted);

        tracker.Paused = true;
        Assert.True(tracker.Paused);
        tracker.Paused = false;

        tracker.SuspendTemprarily(GestureModifier.Scroll);
        Assert.True(tracker.IsSuspended);
        tracker.ResumeSuspension();
        Assert.False(tracker.IsSuspended);

        tracker.Stop();
        Assert.False(hook.IsInstalled);
        Assert.False(tracker.IsStarted);

        tracker.Start();
        Assert.True(hook.IsInstalled);
        tracker.Stop();
    }

    [Fact]
    public void RepeatedDisposeDoesNotThrow()
    {
        var tracker = new PathTracker();
        tracker.Dispose();
        tracker.Dispose();
        Assert.True(tracker.IsDisposed);
    }

    [Fact]
    public void InitialMovementThresholdStartsPathAndMaximumStopsTracking()
    {
        using var hook = new FakeMouseKeyboardHook();
        using var tracker = CreateTracker(hook);
        tracker.InitialStayTimeout = false;
        tracker.MaxGestureSteps = 1;

        var startCount = 0;
        var growCount = 0;
        tracker.PathStart += _ => startCount++;
        tracker.PathGrow += _ => growCount++;

        tracker.Start();
        hook.RaiseMouse(MouseMsg.WMMButtonDown, 100, 100);
        Assert.True(WaitUntil(() => startCount == 0 && growCount == 0));

        hook.RaiseMouse(MouseMsg.WMMouseMove, 106, 100);
        Assert.True(WaitUntil(() => startCount == 1));

        hook.RaiseMouse(MouseMsg.WMMouseMove, 112, 100);
        Assert.True(WaitUntil(() => growCount == 1));
        Thread.Sleep(50);
        Assert.Equal(1, startCount);
        Assert.Equal(1, growCount);

        tracker.Stop();
    }

    [Fact]
    public void JitterBelowInitialMoveDoesNotStartPath()
    {
        using var hook = new FakeMouseKeyboardHook();
        using var tracker = CreateTracker(hook);
        tracker.InitialStayTimeout = false;

        var started = false;
        tracker.PathStart += _ => started = true;

        tracker.Start();
        hook.RaiseMouse(MouseMsg.WMMButtonDown, 100, 100);
        hook.RaiseMouse(MouseMsg.WMMouseMove, 103, 100);
        Thread.Sleep(50);
        Assert.False(started);

        tracker.Stop();
    }

    [Fact]
    public void InputSimulatorPreservesSimulatedEventTag()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke)
        {
            ExtraInfo = (nint)19900620
        };

        Assert.True(simulator.RightClick());
        Assert.All(sender.Inputs, input => Assert.Equal((UIntPtr)19900620, input.Union.Mouse.ExtraInfo));
    }

    [Fact]
    public void KeyboardHookDispatchesDownAndUpEvents()
    {
        using var hook = new FakeMouseKeyboardHook();
        var eventTypes = new List<KeyboardEventType>();
        hook.KeyboardHookEvent += args => eventTypes.Add(args.EventType);

        hook.RaiseKeyboard(KeyboardEventType.KeyDown, VirtualKeyCode.VK_LWIN);
        hook.RaiseKeyboard(KeyboardEventType.KeyUp, VirtualKeyCode.VK_LWIN);

        Assert.Equal([KeyboardEventType.KeyDown, KeyboardEventType.KeyUp], eventTypes);
    }

    private static PathTracker CreateTracker(FakeMouseKeyboardHook hook) => new(
        hook,
        new InputSimulator(_ => 1),
        () => [new Rectangle(0, 0, 1920, 1080)],
        (point, _) => new TestGestureContext
        {
            StartPoint = point,
            EndPoint = point
        },
        () => false);

    private static bool WaitUntil(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            Thread.Sleep(10);
        }

        return condition();
    }

    private sealed class CapturingInputSender
    {
        public List<INPUT> Inputs { get; } = [];

        public uint Invoke(INPUT[] inputs)
        {
            Inputs.AddRange(inputs);
            return (uint)inputs.Length;
        }
    }

    private sealed class TestGestureContext : GestureContext
    {
        public override void ActivateTargetWindow()
        {
        }
    }

    internal sealed class FakeMouseKeyboardHook : IMouseKeyboardHook
    {
        public event Action<MouseHookEventArgs>? MouseHookEvent;
        public event Action<KeyboardHookEventArgs>? KeyboardHookEvent;

        public bool IsInstalled { get; private set; }

        public void Install() => IsInstalled = true;

        public void Uninstall() => IsInstalled = false;

        public void Dispose() => Uninstall();

        public void RaiseMouse(MouseMsg message, int x, int y) =>
            MouseHookEvent?.Invoke(new MouseHookEventArgs(message, x, y, 0, UIntPtr.Zero));

        public void RaiseKeyboard(KeyboardEventType eventType, VirtualKeyCode key) =>
            KeyboardHookEvent?.Invoke(new KeyboardHookEventArgs(eventType, key, UIntPtr.Zero));
    }
}
