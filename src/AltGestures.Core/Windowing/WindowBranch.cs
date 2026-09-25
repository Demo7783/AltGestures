using System.Drawing;
using AltGestures.Core.Arbitration;
using AltGestures.Core.Configuration;
using AltGestures.Core.Input;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 将仲裁后的窗口输入转换成拖拽会话或一次性窗口命令。
/// </summary>
public sealed class WindowBranch : IWindowBranch
{
    private readonly IWindowSystem windowSystem;
    private readonly AppConfig config;
    private readonly WindowActionExecutor executor;
    private WindowMover? mover;
    private WindowResizer? resizer;
    private Timer? updateTimer;

    public WindowBranch(IWindowSystem windowSystem, AppConfig config, InputSimulator inputSimulator)
    {
        this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        executor = new WindowActionExecutor(windowSystem, inputSimulator);
    }

    public void OnMouseDown(
        WindowAction action,
        WindowMouseButton button,
        int x,
        int y,
        WindowDragModifiers modifiers,
        bool isDoubleClick)
    {
        var point = new Point(x, y);
        var target = windowSystem.FindTarget(point);
        if (target is null)
        {
            return;
        }

        if (isDoubleClick && executor.ExecuteDoubleClick(button, target.Value, point))
        {
            return;
        }

        if (action is not (WindowAction.Move or WindowAction.Resize))
        {
            _ = executor.ExecuteClick(action, target.Value, point, modifiers.HasFlag(WindowDragModifiers.Shift));
            return;
        }

        var bounds = windowSystem.GetBounds(target.Value);
        if (bounds is null)
        {
            return;
        }

        var zone = WindowGeometry.GetResizeZone(bounds.Value, point);
        var session = new WindowDragSession(
            target.Value,
            bounds.Value,
            point,
            windowSystem.GetWorkArea(point),
            windowSystem.GetOtherWindows(target.Value),
            zone);
        mover = action == WindowAction.Move ? new WindowMover(windowSystem, config, session) : null;
        resizer = action == WindowAction.Resize ? new WindowResizer(windowSystem, config, session) : null;
        updateTimer ??= new Timer(_ => FlushPendingUpdate());
        updateTimer.Change(0, 100);
        if (config.AutoFocus || modifiers.HasFlag(WindowDragModifiers.Control))
        {
            windowSystem.Focus(target.Value);
        }
    }

    public void OnMouseMove(int x, int y, WindowDragModifiers modifiers)
    {
        if (mover?.Move(new Point(x, y), modifiers) ?? false)
        {
            return;
        }

        _ = resizer?.Resize(new Point(x, y));
    }

    public void OnMouseUp(WindowMouseButton button, int x, int y)
    {
        _ = button;
        _ = x;
        _ = y;
        mover = null;
        resizer = null;
        updateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void OnMouseWheel(int delta, int x, int y, WindowDragModifiers modifiers)
    {
        var action = config.CreateWindowActionBinding().GetWheelAction(delta);
        if (action == WindowAction.None)
        {
            return;
        }

        var target = windowSystem.FindTarget(new Point(x, y));
        _ = executor.ExecuteWheel(
            action,
            target ?? nint.Zero,
            delta,
            modifiers.HasFlag(WindowDragModifiers.Shift));
    }

    private void FlushPendingUpdate()
    {
        var now = Environment.TickCount64;
        _ = mover?.Tick(now);
        _ = resizer?.Tick(now);
    }
}
