// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Runtime.InteropServices;

namespace AltGestures.Core.Interop;

public static class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
    public static extern nint GetModuleHandle(string? moduleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern ulong GetTickCount64();
}
