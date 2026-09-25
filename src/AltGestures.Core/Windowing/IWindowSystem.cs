using System.Drawing;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 提供窗口操作所需的平台能力抽象。
/// </summary>
public interface IWindowSystem
{
    nint? FindTarget(Point point);

    Rectangle? GetBounds(nint window);

    Rectangle GetWorkArea(Point point);

    IReadOnlyList<Rectangle> GetOtherWindows(nint target);

    bool Move(nint window, Rectangle bounds);

    bool Resize(nint window, Rectangle bounds);

    void Close(nint window);

    void Minimize(nint window);

    void Lower(nint window);

    void ToggleAlwaysOnTop(nint window);

    void Center(nint window, Point point);

    void ToggleMaximize(nint window, Point point);

    void MoveToZone(nint window, WindowResizeZone zone, Point point);

    void Focus(nint window);

    int GetTransparency(nint window);

    bool SetTransparency(nint window, int alpha);
}
