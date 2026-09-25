using System.Drawing;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 表示窗口吸附计算结果。
/// </summary>
public readonly record struct SnapResult(Rectangle Bounds, bool Snapped);

/// <summary>
/// 计算窗口与工作区或其他窗口边缘的吸附位置。
/// </summary>
public sealed class SnapEngine(int threshold)
{
    public SnapResult Snap(
        Rectangle bounds,
        Rectangle workArea,
        IReadOnlyList<Rectangle> otherWindows,
        bool snapToWorkArea,
        bool snapToWindows)
    {
        ArgumentNullException.ThrowIfNull(otherWindows);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);

        var result = bounds;
        var snapped = false;

        if (snapToWorkArea)
        {
            var target = ComputeBoundarySnap(bounds, workArea);
            result = target.Bounds;
            snapped |= target.Snapped;
        }

        if (snapToWindows)
        {
            foreach (var other in otherWindows)
            {
                if (other.Width <= 0 || other.Height <= 0)
                {
                    continue;
                }

                var target = ComputeBoundarySnap(result, other);
                if (target.Snapped)
                {
                    result = target.Bounds;
                    snapped = true;
                }
            }
        }

        return new SnapResult(result, snapped);
    }

    private (Rectangle Bounds, bool Snapped) ComputeBoundarySnap(Rectangle bounds, Rectangle target)
    {
        var moved = bounds;
        var snapped = false;

        if (Math.Abs(bounds.Left - target.Left) <= threshold)
        {
            moved.X = target.Left;
            snapped = true;
        }
        else if (Math.Abs(bounds.Right - target.Right) <= threshold)
        {
            moved.X = target.Right - bounds.Width;
            snapped = true;
        }

        if (Math.Abs(bounds.Top - target.Top) <= threshold)
        {
            moved.Y = target.Top;
            snapped = true;
        }
        else if (Math.Abs(bounds.Bottom - target.Bottom) <= threshold)
        {
            moved.Y = target.Bottom - bounds.Height;
            snapped = true;
        }

        return (moved, snapped);
    }
}
