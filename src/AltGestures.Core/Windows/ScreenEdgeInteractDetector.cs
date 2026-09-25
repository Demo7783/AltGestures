// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Drawing;
using AltGestures.Core.Input;
using AltGestures.Core.Interop;

namespace AltGestures.Core.Windows;

public sealed class ScreenEdgeInteractDetector : IDisposable
{
    private const int MinimumRubMove = 80;
    private const int RubRedLineDistance = 50;
    private const int RubTriggerIntervalMilliseconds = 1500;
    private const int RubDirectionIntervalMilliseconds = 600;
    private const int RubMovesRequired = 4;
    private const int CornerTriggerDistance = 2;
    private const int CornerResetDistance = 40;

    private readonly Func<bool> anyMouseButtonIsDown;
    private Rectangle[] screens = [];
    private ScreenEdge? activeRubEdge;
    private int rubTimes;
    private int rubPeakPosition;
    private int lastRubDirection;
    private long lastRubTriggerTime;
    private bool lastCornerWasReset = true;
    private ScreenCorner lastTriggeredCorner;
    private bool disposed;

    public ScreenEdgeInteractDetector(Func<bool>? anyMouseButtonIsDown = null)
    {
        this.anyMouseButtonIsDown = anyMouseButtonIsDown ?? IsAnyMouseButtonDown;
    }

    public event Action<ScreenEdge>? Rub;
    public event Action<ScreenCorner>? HotCorner;

    public bool Paused { get; set; }
    public bool Enabled { get; set; } = true;
    public float DpiScale { get; private set; } = 1.0f;

    public void SetScreens(IEnumerable<Rectangle> screenBounds, float dpiScale = 1.0f)
    {
        ArgumentNullException.ThrowIfNull(screenBounds);
        ArgumentOutOfRangeException.ThrowIfLessThan(dpiScale, 0.1f);
        screens = [.. screenBounds];
        DpiScale = dpiScale;
    }

    public void HandleMouse(MouseHookEventArgs args)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Paused || !Enabled)
        {
            return;
        }

        if (args.Message != MouseMsg.WMMouseMove || anyMouseButtonIsDown())
        {
            return;
        }

        var bounds = FindScreen(new Point(args.X, args.Y));
        if (bounds is null)
        {
            return;
        }

        var position = new Point(args.X - bounds.Value.X, args.Y - bounds.Value.Y);
        var timestamp = Environment.TickCount64;

        var corner = DetectHotCorner(
            new Point(args.X, args.Y),
            bounds.Value,
            lastCornerWasReset,
            lastTriggeredCorner);
        if (corner is not null)
        {
            lastCornerWasReset = false;
            lastTriggeredCorner = corner.Value;
            HotCorner?.Invoke(corner.Value);
        }
        else if (!lastCornerWasReset &&
                 GetDistance(new Point(args.X, args.Y), GetCornerPoint(bounds.Value, lastTriggeredCorner)) > CornerResetDistance)
        {
            lastCornerWasReset = true;
        }

        var edge = DetectRub(position, bounds.Value.Size, timestamp);
        if (edge is not null)
        {
            Rub?.Invoke(edge.Value);
        }

    }

    public void Dispose()
    {
        disposed = true;
        screens = [];
    }

    internal static Rectangle? FindScreen(Point point, IEnumerable<Rectangle> screens)
    {
        foreach (var screen in screens)
        {
            if (screen.Contains(point))
            {
                return screen;
            }
        }

        return null;
    }

    internal static ScreenEdge? GetEdge(
        Point point,
        Size screenSize,
        int edgeThickness = 16,
        int cornerExcludeDistance = 100)
    {
        if (point.X <= edgeThickness &&
            point.Y > cornerExcludeDistance &&
            point.Y < screenSize.Height - cornerExcludeDistance)
        {
            return ScreenEdge.Left;
        }

        if (point.X >= screenSize.Width - edgeThickness &&
            point.Y > cornerExcludeDistance &&
            point.Y < screenSize.Height - cornerExcludeDistance)
        {
            return ScreenEdge.Right;
        }

        if (point.Y <= edgeThickness &&
            point.X > cornerExcludeDistance &&
            point.X < screenSize.Width - cornerExcludeDistance)
        {
            return ScreenEdge.Top;
        }

        if (point.Y >= screenSize.Height - edgeThickness &&
            point.X > cornerExcludeDistance &&
            point.X < screenSize.Width - cornerExcludeDistance)
        {
            return ScreenEdge.Bottom;
        }

        return null;
    }

    internal static ScreenCorner? GetCorner(Point point, Rectangle bounds, int triggerDistance = CornerTriggerDistance)
    {
        if (GetDistance(point, new Point(bounds.Left, bounds.Bottom)) <= triggerDistance)
        {
            return ScreenCorner.LeftBottom;
        }

        if (GetDistance(point, new Point(bounds.Left, bounds.Top)) <= triggerDistance)
        {
            return ScreenCorner.LeftTop;
        }

        if (GetDistance(point, new Point(bounds.Right, bounds.Top)) <= triggerDistance)
        {
            return ScreenCorner.RightTop;
        }

        if (GetDistance(point, new Point(bounds.Right, bounds.Bottom)) <= triggerDistance)
        {
            return ScreenCorner.RightBottom;
        }

        return null;
    }

    internal static ScreenCorner? DetectHotCorner(
        Point point,
        Rectangle bounds,
        bool cornerWasReset,
        ScreenCorner lastCorner,
        int triggerDistance = CornerTriggerDistance,
        int resetDistance = CornerResetDistance)
    {
        return cornerWasReset ? GetCorner(point, bounds, triggerDistance) : null;
    }

    internal static bool HasMovedFarEnough(Point lastPoint, Point currentPoint, int threshold) =>
        GetDistance(lastPoint, currentPoint) >= threshold;

    private ScreenEdge? DetectRub(Point point, Size screenSize, long timestamp)
    {
        var edgeThickness = Math.Max(1, (int)(16 * DpiScale));
        if (activeRubEdge is null)
        {
            activeRubEdge = GetEdge(point, screenSize, edgeThickness);
            if (activeRubEdge is null)
            {
                return null;
            }

            lastRubTriggerTime = timestamp;
            rubPeakPosition = GetPositionOnEdge(activeRubEdge.Value, point);
            rubTimes = 0;
            lastRubDirection = 0;
            return null;
        }

        if (rubTimes >= RubMovesRequired)
        {
            if (GetDistanceToEdge(activeRubEdge.Value, point, screenSize) >= RubRedLineDistance ||
                timestamp - lastRubTriggerTime > RubTriggerIntervalMilliseconds)
            {
                activeRubEdge = null;
            }

            return null;
        }

        if (GetEdge(point, screenSize, edgeThickness) != activeRubEdge)
        {
            activeRubEdge = null;
            return null;
        }

        var position = GetPositionOnEdge(activeRubEdge.Value, point);
        var distance = position - rubPeakPosition;
        if (Math.Sign(distance) == Math.Sign(lastRubDirection) && lastRubDirection != 0)
        {
            rubPeakPosition = position;
            return null;
        }

        if (Math.Abs(distance) < MinimumRubMove)
        {
            return null;
        }

        if (timestamp - lastRubTriggerTime > RubDirectionIntervalMilliseconds)
        {
            activeRubEdge = null;
            return null;
        }

        lastRubTriggerTime = timestamp;
        rubTimes++;
        rubPeakPosition = position;
        lastRubDirection = Math.Sign(distance);
        return rubTimes >= RubMovesRequired ? activeRubEdge.Value : null;
    }

    private Rectangle? FindScreen(Point point) => FindScreen(point, screens);

    private static int GetDistance(Point first, Point second)
    {
        var deltaX = first.X - second.X;
        var deltaY = first.Y - second.Y;
        return (int)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private static Point GetCornerPoint(Rectangle bounds, ScreenCorner corner) => corner switch
    {
        ScreenCorner.LeftBottom => new(bounds.Left, bounds.Bottom),
        ScreenCorner.LeftTop => new(bounds.Left, bounds.Top),
        ScreenCorner.RightTop => new(bounds.Right, bounds.Top),
        ScreenCorner.RightBottom => new(bounds.Right, bounds.Bottom),
        _ => throw new NotSupportedException($"未知屏幕角落：{corner}。")
    };

    private static int GetPositionOnEdge(ScreenEdge edge, Point point) =>
        edge is ScreenEdge.Left or ScreenEdge.Right ? point.Y : point.X;

    private static int GetDistanceToEdge(ScreenEdge edge, Point point, Size screenSize) => edge switch
    {
        ScreenEdge.Left => point.X,
        ScreenEdge.Right => screenSize.Width - point.X,
        ScreenEdge.Top => point.Y,
        ScreenEdge.Bottom => screenSize.Height - point.Y,
        _ => throw new NotSupportedException($"未知屏幕边缘：{edge}。")
    };

    private static bool IsAnyMouseButtonDown() =>
        (User32.GetAsyncKeyState((int)VirtualKeyCode.VK_LBUTTON) < 0) ||
        (User32.GetAsyncKeyState((int)VirtualKeyCode.VK_RBUTTON) < 0);

}
