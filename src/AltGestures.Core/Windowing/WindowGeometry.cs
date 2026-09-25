using System.Drawing;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 表示窗口缩放时鼠标所在的区域。
/// </summary>
[Flags]
public enum WindowResizeZone
{
    None = 0,
    Left = 1,
    Right = 2,
    Top = 4,
    Bottom = 8,
    Center = 16
}

/// <summary>
/// 提供与 Win32 解耦的窗口几何计算。
/// </summary>
public static class WindowGeometry
{
    public static WindowResizeZone GetResizeZone(Rectangle bounds, Point cursor)
    {
        if (!bounds.Contains(cursor))
        {
            return WindowResizeZone.None;
        }

        var border = Math.Max(8, Math.Min(bounds.Width, bounds.Height) / 10);
        var nearLeft = cursor.X - bounds.Left <= border;
        var nearRight = bounds.Right - 1 - cursor.X <= border;
        var nearTop = cursor.Y - bounds.Top <= border;
        var nearBottom = bounds.Bottom - 1 - cursor.Y <= border;

        var zone = WindowResizeZone.None;
        if (nearLeft) zone |= WindowResizeZone.Left;
        if (nearRight) zone |= WindowResizeZone.Right;
        if (nearTop) zone |= WindowResizeZone.Top;
        if (nearBottom) zone |= WindowResizeZone.Bottom;

        return zone == WindowResizeZone.None ? WindowResizeZone.Center : zone;
    }

    public static Rectangle GetResizedBounds(
        Rectangle original,
        Point start,
        Point current,
        WindowResizeZone zone,
        int minimumSize = 96)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumSize, 1);
        var deltaX = current.X - start.X;
        var deltaY = current.Y - start.Y;
        var result = original;

        if (zone.HasFlag(WindowResizeZone.Left))
        {
            result.X = Math.Min(original.Left + deltaX, original.Right - minimumSize);
            result.Width = original.Right - result.X;
        }

        if (zone.HasFlag(WindowResizeZone.Right))
        {
            result.Width = Math.Max(minimumSize, original.Width + deltaX);
        }

        if (zone.HasFlag(WindowResizeZone.Top))
        {
            result.Y = Math.Min(original.Top + deltaY, original.Bottom - minimumSize);
            result.Height = original.Bottom - result.Y;
        }

        if (zone.HasFlag(WindowResizeZone.Bottom))
        {
            result.Height = Math.Max(minimumSize, original.Height + deltaY);
        }

        if (zone == WindowResizeZone.Center)
        {
            result.X = original.Left - (deltaX / 2);
            result.Width = original.Width + deltaX;
            result.Y = original.Top - (deltaY / 2);
            result.Height = original.Height + deltaY;
            if (result.Width < minimumSize || result.Height < minimumSize)
            {
                return original;
            }
        }

        return result;
    }
}
