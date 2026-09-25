// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

namespace AltGestures.Core;

[Flags]
public enum GestureModifier
{
    None = 0,
    WheelForward = 1,
    WheelBackward = 2,
    MiddleButtonDown = 4,
    LeftButtonDown = 8,
    RightButtonDown = 16,
    X1 = 32,
    X2 = 64,
    Scroll = WheelBackward | WheelForward,
    All = WheelForward | WheelBackward | MiddleButtonDown | LeftButtonDown | RightButtonDown
}

public static class GestureModifierHelper
{
    public static string ToMnemonic(this GestureModifier modifier) => modifier switch
    {
        GestureModifier.WheelForward => "▲",
        GestureModifier.WheelBackward => "▼",
        GestureModifier.MiddleButtonDown => "●",
        GestureModifier.LeftButtonDown => "◐",
        GestureModifier.RightButtonDown => "◑",
        GestureModifier.X1 => "X1",
        GestureModifier.X2 => "X2",
        _ => string.Empty
    };
}
