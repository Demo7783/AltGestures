using System.Drawing;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 保存一次窗口拖拽或缩放会话的不可变输入状态。
/// </summary>
public sealed record WindowDragSession(
    nint Window,
    Rectangle OriginalBounds,
    Point StartPoint,
    Rectangle WorkArea,
    IReadOnlyList<Rectangle> OtherWindows,
    WindowResizeZone ResizeZone);

