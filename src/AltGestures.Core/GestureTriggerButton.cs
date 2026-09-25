// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

namespace AltGestures.Core;

[Flags]
public enum GestureTriggerButton
{
    None = 0, Right = 1, Middle = 2, X1 = 4, X2 = 8,
    X = X1 | X2
}

public static class GestureTriggerButtonExtension
{
    public static string ToMnemonic(this GestureTriggerButton gestureBtn) => gestureBtn switch
    {
        GestureTriggerButton.Middle => "●",
        GestureTriggerButton.Right => "◑",
        GestureTriggerButton.X1 => "X1",
        GestureTriggerButton.X2 => "X2",
        _ => string.Empty
    };
}
