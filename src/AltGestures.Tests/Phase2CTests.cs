using System.Drawing;
using AltGestures.Core;
using AltGestures.Core.Arbitration;
using AltGestures.Core.Configuration;
using AltGestures.Core.Input;
using AltGestures.Core.Windowing;
using AltGestures.Core.Windows;
using Xunit;

namespace AltGestures.Tests;

public sealed class Phase2CTests
{
    [Fact]
    public void WindowActionEnumHasElevenValues()
    {
        Assert.Equal(11, Enum.GetValues<WindowAction>().Length);
        Assert.Equal(
            [
                WindowAction.None,
                WindowAction.Move,
                WindowAction.Resize,
                WindowAction.Close,
                WindowAction.Minimize,
                WindowAction.Lower,
                WindowAction.AlwaysOnTop,
                WindowAction.Center,
                WindowAction.AltTab,
                WindowAction.Volume,
                WindowAction.Transparency
            ],
            Enum.GetValues<WindowAction>());
    }

    [Fact]
    public void BindingParsesActionsAndReturnsNoneForUnboundKeys()
    {
        var binding = WindowActionBinding.FromNames(new Dictionary<string, string>
        {
            ["LMB"] = "Move",
            ["MMB"] = "Resize",
            ["RMB"] = "Resize",
            ["MB4"] = "Nothing",
            ["Scroll"] = "AltTab"
        });

        Assert.Equal(WindowAction.Move, binding[WindowTrigger.Left]);
        Assert.Equal(WindowAction.Resize, binding[WindowTrigger.Middle]);
        Assert.Equal(WindowAction.Resize, binding[WindowTrigger.Right]);
        Assert.Equal(WindowAction.None, binding[WindowTrigger.X1]);
        Assert.Equal(WindowAction.AltTab, binding.GetWheelAction(120));
        Assert.Equal(WindowAction.None, binding[WindowTrigger.X2]);
    }

    [Theory]
    [InlineData(0, 150, WindowResizeZone.Left)]
    [InlineData(399, 150, WindowResizeZone.Right)]
    [InlineData(200, 0, WindowResizeZone.Top)]
    [InlineData(200, 299, WindowResizeZone.Bottom)]
    [InlineData(0, 0, WindowResizeZone.Left | WindowResizeZone.Top)]
    [InlineData(399, 0, WindowResizeZone.Right | WindowResizeZone.Top)]
    [InlineData(0, 299, WindowResizeZone.Left | WindowResizeZone.Bottom)]
    [InlineData(399, 299, WindowResizeZone.Right | WindowResizeZone.Bottom)]
    [InlineData(200, 150, WindowResizeZone.Center)]
    public void GeometryDetectsEdgesCornersAndCenter(int x, int y, WindowResizeZone expected)
    {
        Assert.Equal(expected, WindowGeometry.GetResizeZone(new Rectangle(0, 0, 400, 300), new Point(x, y)));
    }

    [Fact]
    public void SnapEngineSnapsToLeftRightAndTopEdges()
    {
        var engine = new SnapEngine(20);
        var workArea = new Rectangle(0, 0, 1920, 1040);

        Assert.True(engine.Snap(new Rectangle(12, 100, 400, 300), workArea, [], true, false).Snapped);
        Assert.Equal(0, engine.Snap(new Rectangle(12, 100, 400, 300), workArea, [], true, false).Bounds.X);

        Assert.True(engine.Snap(new Rectangle(1512, 100, 400, 300), workArea, [], true, false).Snapped);
        Assert.Equal(1520, engine.Snap(new Rectangle(1512, 100, 400, 300), workArea, [], true, false).Bounds.X);

        Assert.True(engine.Snap(new Rectangle(100, 15, 400, 300), workArea, [], true, false).Snapped);
        Assert.Equal(0, engine.Snap(new Rectangle(100, 15, 400, 300), workArea, [], true, false).Bounds.Y);

        Assert.False(engine.Snap(new Rectangle(30, 100, 400, 300), workArea, [], true, false).Snapped);
    }

    [Fact]
    public void MoveThrottleUpdatesOnlyAfterAccumulatedDistanceAndResets()
    {
        var system = new FakeWindowSystem();
        var session = new WindowDragSession(
            1,
            new Rectangle(100, 100, 400, 300),
            new Point(10, 10),
            new Rectangle(0, 0, 1920, 1040),
            [],
            WindowResizeZone.None);
        var mover = new WindowMover(system, new AppConfig { Aero = false }, session);

        Assert.False(mover.Move(new Point(11, 10), WindowDragModifiers.None));
        Assert.Empty(system.MovedBounds);

        Assert.True(mover.Move(new Point(13, 10), WindowDragModifiers.None));
        Assert.Equal([new Rectangle(103, 100, 400, 300)], system.MovedBounds);

        Assert.False(mover.Move(new Point(14, 10), WindowDragModifiers.None));
        Assert.True(mover.Move(new Point(17, 10), WindowDragModifiers.None));
        Assert.Equal(2, system.MovedBounds.Count);
    }

    [Fact]
    public void AltBoundMouseDownGoesToWindowBranchAndIsHandled()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        using var arbiter = CreateArbiter(hook, branch, () => ModifierModifiers.LeftAlt);
        MouseHookEventArgs? forwarded = null;
        arbiter.MouseHookEvent += args => forwarded = args;

        var args = Mouse(MouseMsg.WMLButtonDown);
        hook.RaiseMouse(args);

        Assert.True(args.Handled);
        Assert.Null(forwarded);
        Assert.Equal(InputOwnership.Window, arbiter.Ownership);
        Assert.Equal(ArbitrationState.WindowDragging, arbiter.State);
        Assert.Equal([(WindowAction.Move, WindowMouseButton.Left, 10, 10, false)], branch.Presses);
    }

    [Fact]
    public void MouseWithoutAltIsForwardedToGestureBranch()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        using var arbiter = CreateArbiter(hook, branch, () => ModifierModifiers.None);
        var forwarded = new List<MouseHookEventArgs>();
        arbiter.MouseHookEvent += args => forwarded.Add(args);

        var args = Mouse(MouseMsg.WMRButtonDown);
        hook.RaiseMouse(args);

        Assert.False(args.Handled);
        Assert.Same(args, Assert.Single(forwarded));
        Assert.Empty(branch.Presses);
        Assert.Equal(InputOwnership.Gesture, arbiter.Ownership);
    }

    [Fact]
    public void AltWithUnboundActionIsPassedThrough()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        using var arbiter = CreateArbiter(
            hook,
            branch,
            () => ModifierModifiers.LeftAlt,
            new WindowActionBinding(new Dictionary<WindowTrigger, WindowAction>()));
        var forwarded = new List<MouseHookEventArgs>();
        arbiter.MouseHookEvent += args => forwarded.Add(args);

        var args = Mouse(MouseMsg.WMXButtonDown, mouseData: 1u << 16);
        hook.RaiseMouse(args);

        Assert.False(args.Handled);
        Assert.Single(forwarded);
        Assert.Empty(branch.Presses);
        Assert.Equal(InputOwnership.Gesture, arbiter.Ownership);
    }

    [Fact]
    public void DragOwnershipStaysWithWindowAfterAltIsReleased()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        var altDown = true;
        using var arbiter = CreateArbiter(hook, branch, () => altDown ? ModifierModifiers.LeftAlt : ModifierModifiers.None);
        arbiter.MouseHookEvent += _ => Assert.Fail("窗口拖拽中的事件不应转给手势分支。");

        var down = Mouse(MouseMsg.WMLButtonDown);
        hook.RaiseMouse(down);
        altDown = false;
        var move = Mouse(MouseMsg.WMMouseMove, 40, 40);
        hook.RaiseMouse(move);
        var up = Mouse(MouseMsg.WMLButtonUp, 42, 42);
        hook.RaiseMouse(up);

        Assert.True(down.Handled);
        Assert.True(move.Handled);
        Assert.True(up.Handled);
        Assert.Equal([(WindowAction.Move, WindowMouseButton.Left, 10, 10, false)], branch.Presses);
        Assert.Equal([(40, 40)], branch.Moves);
        Assert.Equal(ArbitrationState.Idle, arbiter.State);
    }

    [Fact]
    public void GestureOwnershipIsNotStolenByLateAlt()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        var altDown = false;
        using var arbiter = CreateArbiter(hook, branch, () => altDown ? ModifierModifiers.LeftAlt : ModifierModifiers.None);
        var forwarded = new List<MouseHookEventArgs>();
        arbiter.MouseHookEvent += args => forwarded.Add(args);

        hook.RaiseMouse(Mouse(MouseMsg.WMRButtonDown));
        altDown = true;
        hook.RaiseKeyboard(KeyboardEventType.KeyDown, VirtualKeyCode.VK_LMENU);
        hook.RaiseMouse(Mouse(MouseMsg.WMMouseMove, 50, 50));
        hook.RaiseMouse(Mouse(MouseMsg.WMRButtonUp, 52, 52));

        Assert.Equal(3, forwarded.Count);
        Assert.Empty(branch.Presses);
        Assert.Empty(branch.Moves);
        Assert.Equal(InputOwnership.None, arbiter.Ownership);
    }

    [Fact]
    public void SecondClickWithinSystemDoubleClickTimeIsDoubleClick()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        var time = 0L;
        using var arbiter = CreateArbiter(
            hook,
            branch,
            () => ModifierModifiers.LeftAlt,
            timestamp: () => time,
            doubleClickTime: () => 500);

        time = 100;
        hook.RaiseMouse(Mouse(MouseMsg.WMLButtonDown));
        hook.RaiseMouse(Mouse(MouseMsg.WMLButtonUp));
        time = 400;
        hook.RaiseMouse(Mouse(MouseMsg.WMLButtonDown));
        hook.RaiseMouse(Mouse(MouseMsg.WMLButtonUp));

        Assert.False(branch.Presses[0].IsDoubleClick);
        Assert.True(branch.Presses[1].IsDoubleClick);
    }

    [Fact]
    public void AppConfigRoundTripsThroughConfigStore()
    {
        var path = Path.Combine(Path.GetTempPath(), $"altgestures-{Guid.NewGuid():N}.json");
        try
        {
            var config = new AppConfig
            {
                WindowActions = new Dictionary<string, string> { ["Left"] = "Close", ["Scroll"] = "Volume" },
                SnapThreshold = 31,
                MoveRate = 4,
                ResizeRate = 9,
                AutoFocus = true,
                Aero = false
            };
            new ConfigStore(path).Save(config);
            var loaded = new ConfigStore(path).Load();

            Assert.Equal(config.WindowActions, loaded.WindowActions);
            Assert.Equal(config.SnapThreshold, loaded.SnapThreshold);
            Assert.Equal(config.MoveRate, loaded.MoveRate);
            Assert.Equal(config.ResizeRate, loaded.ResizeRate);
            Assert.Equal(config.AutoFocus, loaded.AutoFocus);
            Assert.Equal(config.Aero, loaded.Aero);
            Assert.Equal(WindowAction.Close, loaded.CreateWindowActionBinding()[WindowTrigger.Left]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RepeatedArbiterDisposeDoesNotThrow()
    {
        using var hook = new FakeMouseKeyboardHook();
        var arbiter = CreateArbiter(hook, new FakeWindowBranch(), () => ModifierModifiers.None);
        arbiter.Dispose();
        arbiter.Dispose();
    }

    [Fact]
    public void ArbiterCanInjectIntoPathTrackerAndForwardGestureInput()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        using var arbiter = CreateArbiter(hook, branch, () => ModifierModifiers.None);
        using var tracker = new PathTracker(
            arbiter,
            new InputSimulator(_ => 1),
            () => [new Rectangle(0, 0, 1920, 1080)],
            (point, _) => new Win32GestureContext { StartPoint = point, EndPoint = point },
            () => false)
        {
            TriggerButton = GestureTriggerButton.Middle,
            InitialStayTimeout = false
        };
        var forwarded = 0;
        var beforeStarts = 0;
        tracker.BeforePathStart += _ => beforeStarts++;
        arbiter.MouseHookEvent += _ => forwarded++;

        tracker.Start();
        hook.RaiseMouse(Mouse(MouseMsg.WMMButtonDown, 100, 100));
        hook.RaiseMouse(Mouse(MouseMsg.WMMouseMove, 120, 100));
        hook.RaiseMouse(Mouse(MouseMsg.WMMButtonUp, 130, 100));

        Assert.Equal(3, forwarded);
        Assert.True(WaitUntil(() => beforeStarts == 1), $"beforeStarts={beforeStarts}");
        Assert.Empty(branch.Presses);
        Assert.Empty(branch.Moves);
    }

    private static InputArbiter CreateArbiter(
        FakeMouseKeyboardHook hook,
        FakeWindowBranch branch,
        Func<ModifierModifiers> modifiers,
        WindowActionBinding? bindings = null,
        Func<long>? timestamp = null,
        Func<uint>? doubleClickTime = null) => new(
            hook,
            branch,
            bindings ?? WindowActionBinding.Default,
            modifiers,
            timestamp,
            doubleClickTime ?? (() => 500),
            dispatchSynchronously: true);

    private static MouseHookEventArgs Mouse(
        MouseMsg message,
        int x = 10,
        int y = 10,
        uint mouseData = 0) => new(message, x, y, mouseData, UIntPtr.Zero);

    private static bool WaitUntil(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            Thread.Sleep(10);
        }

        return condition();
    }

    private sealed class FakeWindowBranch : IWindowBranch
    {
        public List<(WindowAction Action, WindowMouseButton Button, int X, int Y, bool IsDoubleClick)> Presses { get; } = [];

        public List<(int X, int Y)> Moves { get; } = [];

        public void OnMouseDown(
            WindowAction action,
            WindowMouseButton button,
            int x,
            int y,
            WindowDragModifiers modifiers,
            bool isDoubleClick) => Presses.Add((action, button, x, y, isDoubleClick));

        public void OnMouseMove(int x, int y, WindowDragModifiers modifiers) => Moves.Add((x, y));

        public void OnMouseUp(WindowMouseButton button, int x, int y)
        {
        }

        public void OnMouseWheel(int delta, int x, int y, WindowDragModifiers modifiers)
        {
        }

    }

    private sealed class FakeWindowSystem : IWindowSystem
    {
        public List<Rectangle> MovedBounds { get; } = [];

        public nint? FindTarget(Point point) => 1;

        public Rectangle? GetBounds(nint window) => new(100, 100, 400, 300);

        public Rectangle GetWorkArea(Point point) => new(0, 0, 1920, 1040);

        public IReadOnlyList<Rectangle> GetOtherWindows(nint target) => [];

        public bool Move(nint window, Rectangle bounds)
        {
            MovedBounds.Add(bounds);
            return true;
        }

        public bool Resize(nint window, Rectangle bounds) => true;

        public void Close(nint window)
        {
        }

        public void Minimize(nint window)
        {
        }

        public void Lower(nint window)
        {
        }

        public void ToggleAlwaysOnTop(nint window)
        {
        }

        public void Center(nint window, Point point)
        {
        }

        public void ToggleMaximize(nint window, Point point)
        {
        }

        public void MoveToZone(nint window, WindowResizeZone zone, Point point)
        {
        }

        public void Focus(nint window)
        {
        }

        public int GetTransparency(nint window) => 255;

        public bool SetTransparency(nint window, int alpha) => true;
    }

    private sealed class FakeMouseKeyboardHook : IMouseKeyboardHook
    {
        public event Action<MouseHookEventArgs>? MouseHookEvent;

        public event Action<KeyboardHookEventArgs>? KeyboardHookEvent;

        public bool IsInstalled => true;

        public void Install()
        {
        }

        public void Uninstall()
        {
        }

        public void Dispose()
        {
        }

        public void RaiseMouse(MouseHookEventArgs args) => MouseHookEvent?.Invoke(args);

        public void RaiseKeyboard(KeyboardEventType eventType, VirtualKeyCode key) =>
            KeyboardHookEvent?.Invoke(new KeyboardHookEventArgs(eventType, key, UIntPtr.Zero));
    }
}
