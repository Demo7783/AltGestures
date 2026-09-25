namespace AltGestures.Core.Windowing;

/// <summary>
/// 表示 Alt 修饰下鼠标按键或滚轮触发的窗口动作。
/// </summary>
public enum WindowAction
{
    None,
    Move,
    Resize,
    Close,
    Minimize,
    Lower,
    AlwaysOnTop,
    Center,
    AltTab,
    Volume,
    Transparency
}

/// <summary>
/// 表示可绑定窗口动作的物理鼠标触发器。
/// </summary>
public enum WindowTrigger
{
    Left,
    Middle,
    Right,
    X1,
    X2,
    Wheel
}

/// <summary>
/// 表示参与窗口拖拽的物理鼠标键。
/// </summary>
public enum WindowMouseButton
{
    Left,
    Middle,
    Right,
    X1,
    X2
}
