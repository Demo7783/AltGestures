// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

namespace AltGestures.Core.Windows;

using AltGestures.Core.Interop;

public enum MouseMsg
{
    WMMouseMove = 0x0200,
    WM_MOUSEMOVE = WMMouseMove,
    WMLButtonDown = 0x0201,
    WM_LBUTTONDOWN = WMLButtonDown,
    WMLButtonUp = 0x0202,
    WM_LBUTTONUP = WMLButtonUp,
    WMRButtonDown = 0x0204,
    WM_RBUTTONDOWN = WMRButtonDown,
    WMRButtonUp = 0x0205,
    WM_RBUTTONUP = WMRButtonUp,
    WMMouseWheel = 0x020A,
    WM_MOUSEWHEEL = WMMouseWheel,
    WMMButtonDown = 0x0207,
    WM_MBUTTONDOWN = WMMButtonDown,
    WMMButtonUp = 0x0208,
    WM_MBUTTONUP = WMMButtonUp,
    WMXButtonDown = 0x020B,
    WM_XBUTTONDOWN = WMXButtonDown,
    WMXButtonUp = 0x020C,
    WM_XBUTTONUP = WMXButtonUp
}

public enum KeyboardEventType
{
    KeyDown,
    KeyUp
}

public enum XButtonNumber
{
    One = 1,
    Two = 2
}

internal static class MouseMsgMap
{
    internal static MouseMsg? FromWindowsMessage(int message) => message switch
    {
        0x0200 => MouseMsg.WMMouseMove,
        0x0201 => MouseMsg.WMLButtonDown,
        0x0202 => MouseMsg.WMLButtonUp,
        0x0204 => MouseMsg.WMRButtonDown,
        0x0205 => MouseMsg.WMRButtonUp,
        0x0207 => MouseMsg.WMMButtonDown,
        0x0208 => MouseMsg.WMMButtonUp,
        0x020A => MouseMsg.WMMouseWheel,
        0x020B => MouseMsg.WMXButtonDown,
        0x020C => MouseMsg.WMXButtonUp,
        _ => null
    };

    internal static int ToWindowsMessage(MouseMsg message) => (int)message;

    internal static XButtonNumber FromNativeMouseData(uint mouseData) =>
        (mouseData >> 16) switch
        {
            1 => XButtonNumber.One,
            2 => XButtonNumber.Two,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mouseData),
                mouseData,
                "仅支持 XBUTTON1 与 XBUTTON2。")
        };

    internal static KeyboardEventType? FromKeyboardWindowsMessage(int message) => message switch
    {
        NativeConstants.WM_KEYDOWN or NativeConstants.WM_SYSKEYDOWN => KeyboardEventType.KeyDown,
        NativeConstants.WM_KEYUP or NativeConstants.WM_SYSKEYUP => KeyboardEventType.KeyUp,
        _ => null
    };
}
