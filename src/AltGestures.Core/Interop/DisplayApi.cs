// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Runtime.InteropServices;

namespace AltGestures.Core.Interop;

public static class DisplayApi
{
    public delegate bool MonitorEnumProc(nint monitor, nint deviceContext, ref RECT rect, nint data);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRect,
        MonitorEnumProc callback,
        nint data);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint MonitorFromPoint(POINT point, uint flags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(nint monitor, ref MONITORINFO info);
}
