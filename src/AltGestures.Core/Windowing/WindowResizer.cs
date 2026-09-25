using System.Drawing;
using AltGestures.Core.Configuration;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 根据按下时的边角区域缩放窗口，中心区域执行对称缩放。
/// </summary>
public sealed class WindowResizer
{
    private readonly IWindowSystem windowSystem;
    private readonly AppConfig config;
    private readonly WindowDragSession session;
    private readonly object updateLock = new();
    private Point lastAppliedPoint;
    private Point lastReceivedPoint;
    private long lastUpdateTimestamp;

    public WindowResizer(IWindowSystem windowSystem, AppConfig config, WindowDragSession session)
    {
        this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        lastAppliedPoint = session.StartPoint;
        lastReceivedPoint = session.StartPoint;
        lastUpdateTimestamp = Environment.TickCount64;
    }

    public bool Resize(Point point)
    {
        lock (updateLock)
        {
            lastReceivedPoint = point;
            return ResizeCore(point, forceUpdate: false);
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

            return ResizeCore(lastReceivedPoint, forceUpdate: true);
        }
    }

    private bool ResizeCore(Point point, bool forceUpdate)
    {
        var deltaX = point.X - lastAppliedPoint.X;
        var deltaY = point.Y - lastAppliedPoint.Y;
        if (!forceUpdate &&
            (deltaX * deltaX) + (deltaY * deltaY) < config.ResizeRate * config.ResizeRate)
        {
            return false;
        }

        var bounds = WindowGeometry.GetResizedBounds(
            session.OriginalBounds,
            session.StartPoint,
            point,
            session.ResizeZone);
        var resized = windowSystem.Resize(session.Window, bounds);
        if (resized)
        {
            lastAppliedPoint = point;
            lastUpdateTimestamp = Environment.TickCount64;
        }

        return resized;
    }
}
