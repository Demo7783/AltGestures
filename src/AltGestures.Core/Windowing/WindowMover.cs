using System.Drawing;
using AltGestures.Core.Configuration;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 按位移阈值移动窗口，并应用工作区与其他窗口吸附。
/// </summary>
public sealed class WindowMover
{
    private readonly IWindowSystem windowSystem;
    private readonly AppConfig config;
    private readonly WindowDragSession session;
    private readonly SnapEngine snapEngine;
    private readonly object updateLock = new();
    private Point lastAppliedPoint;
    private Point lastReceivedPoint;
    private WindowDragModifiers lastReceivedModifiers;
    private long lastUpdateTimestamp;

    public WindowMover(IWindowSystem windowSystem, AppConfig config, WindowDragSession session)
    {
        this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        snapEngine = new SnapEngine(config.SnapThreshold);
        lastAppliedPoint = session.StartPoint;
        lastReceivedPoint = session.StartPoint;
        lastReceivedModifiers = WindowDragModifiers.None;
        lastUpdateTimestamp = Environment.TickCount64;
    }

    public bool Move(Point point, WindowDragModifiers modifiers)
    {
        lock (updateLock)
        {
            lastReceivedPoint = point;
            lastReceivedModifiers = modifiers;
            return MoveCore(point, forceUpdate: false, modifiers);
        }
    }

    public bool Tick(long timestamp)
    {
        lock (updateLock)
        {
            if (timestamp - lastUpdateTimestamp < 100 || lastReceivedPoint == lastAppliedPoint)
            {
                return false;
            }

            return MoveCore(lastReceivedPoint, forceUpdate: true, lastReceivedModifiers);
        }
    }

    private bool MoveCore(Point point, bool forceUpdate, WindowDragModifiers modifiers)
    {
        var deltaX = point.X - lastAppliedPoint.X;
        var deltaY = point.Y - lastAppliedPoint.Y;
        if (!forceUpdate &&
            (deltaX * deltaX) + (deltaY * deltaY) < config.MoveRate * config.MoveRate)
        {
            return false;
        }

        var desired = new Rectangle(
            session.OriginalBounds.X + point.X - session.StartPoint.X,
            session.OriginalBounds.Y + point.Y - session.StartPoint.Y,
            session.OriginalBounds.Width,
            session.OriginalBounds.Height);
        var snapped = snapEngine.Snap(
            desired,
            session.WorkArea,
            session.OtherWindows,
            config.Aero && !modifiers.HasFlag(WindowDragModifiers.Space),
            modifiers.HasFlag(WindowDragModifiers.Shift));
        var moved = windowSystem.Move(session.Window, snapped.Bounds);
        if (moved)
        {
            lastAppliedPoint = point;
            lastUpdateTimestamp = Environment.TickCount64;
        }

        if (moved && (config.AutoFocus || modifiers.HasFlag(WindowDragModifiers.Control)))
        {
            windowSystem.Focus(session.Window);
        }

        return moved;
    }
}
