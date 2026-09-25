// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Runtime.InteropServices;

namespace AltGestures.Core.Interop;

public static class Shell32
{
    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "ShellExecuteExW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShellExecuteEx(ref ShellExecuteInfo executeInfo);
}
