namespace AltGestures.Core.Windowing;

/// <summary>
/// 表示拖拽过程中生效的修饰键。
/// </summary>
[Flags]
public enum WindowDragModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Space = 4
}
