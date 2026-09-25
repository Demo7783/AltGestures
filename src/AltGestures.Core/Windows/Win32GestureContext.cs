// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Diagnostics;
using System.Drawing;
using System.Text;
using AltGestures.Core.Interop;

namespace AltGestures.Core.Windows;

public sealed class Win32GestureContext : GestureContext
{
    public string ProcessName { get; private set; } = string.Empty;
    public string WindowClassName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;

    public static Win32GestureContext Capture(Point point, bool preferWindowUnderCursor)
    {
        var window = preferWindowUnderCursor
            ? User32.WindowFromPoint(new POINT(point.X, point.Y))
            : User32.GetForegroundWindow();
        window = User32.GetAncestor(window, NativeConstants.GA_ROOT);

        var context = new Win32GestureContext
        {
            WinId = window,
            StartPoint = point,
            EndPoint = point
        };
        context.ReadWindowInformation();
        return context;
    }

    public override void ActivateTargetWindow()
    {
        var rootWindow = User32.GetAncestor(WinId, NativeConstants.GA_ROOT);
        if (rootWindow != nint.Zero && User32.GetForegroundWindow() != rootWindow)
        {
            _ = User32.SetForegroundWindow(rootWindow);
        }
    }

    private void ReadWindowInformation()
    {
        if (WinId == nint.Zero)
        {
            return;
        }

        User32.GetWindowThreadProcessId(WinId, out var processId);
        ProcId = processId;
        if (processId != 0)
        {
            using var process = Process.GetProcessById((int)processId);
            ProcessName = process.ProcessName;
        }

        var className = new StringBuilder(256);
        if (User32.GetClassName(WinId, className, className.Capacity) > 0)
        {
            WindowClassName = className.ToString();
        }

        var title = new StringBuilder(512);
        if (User32.GetWindowText(WinId, title, title.Capacity) > 0)
        {
            Title = title.ToString();
        }
    }
}
