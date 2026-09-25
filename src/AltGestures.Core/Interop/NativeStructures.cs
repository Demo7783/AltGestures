// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Runtime.InteropServices;

namespace AltGestures.Core.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;

    public POINT(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SIZE
{
    public int Width;
    public int Height;

    public SIZE(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public RECT(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct MONITORINFO
{
    public int Size;
    public RECT Monitor;
    public RECT Work;
    public uint Flags;

    public static MONITORINFO Create() => new()
    {
        Size = System.Runtime.CompilerServices.Unsafe.SizeOf<MONITORINFO>()
    };
}

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    public int Dx;
    public int Dy;
    public uint MouseData;
    public uint Flags;
    public uint Time;
    public uint ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    public ushort VirtualKey;
    public ushort ScanCode;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct HARDWAREINPUT
{
    public uint Message;
    public ushort ParameterL;
    public ushort ParameterH;
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT Mouse;
    [FieldOffset(0)] public KEYBDINPUT Keyboard;
    [FieldOffset(0)] public HARDWAREINPUT Hardware;
}

[StructLayout(LayoutKind.Sequential)]
public struct INPUT
{
    public uint Type;
    public InputUnion Union;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct ShellExecuteInfo
{
    public int Size;
    public uint Mask;
    public nint Window;
    public string Verb;
    public string File;
    public string Parameters;
    public string Directory;
    public int Show;
    public nint InstanceApplication;
    public nint Process;
    public nint ProcessId;
}
