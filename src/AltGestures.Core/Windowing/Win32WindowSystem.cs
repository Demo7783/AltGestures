using System.Drawing;
using AltGestures.Core.Interop;
using AltGestures.Core.Windows;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 使用通用 Win32 API 实现窗口操作。所有本机调用都来自 Interop 层。
/// </summary>
/// <remarks>若目标窗口以管理员权限运行，普通权限进程的窗口操作会被 Windows UIPI 静默丢弃；本类型不尝试绕过该系统边界。</remarks>
public sealed class Win32WindowSystem : IWindowSystem
{
    private readonly nint desktopWindow;
    private readonly nint shellWindow;
    private readonly uint currentProcessId;

    public Win32WindowSystem()
    {
        desktopWindow = User32.GetDesktopWindow();
        shellWindow = User32.GetShellWindow();
        currentProcessId = (uint)Environment.ProcessId;
    }

    public nint? FindTarget(Point point)
    {
        var window = User32.WindowFromPoint(new POINT(point.X, point.Y));
        if (window == nint.Zero)
        {
            return null;
        }

        var root = User32.GetAncestor(window, NativeConstants.GA_ROOT);
        if (root == nint.Zero)
        {
            root = window;
        }

        if (root == desktopWindow || root == shellWindow || IsOwnWindow(root) || !User32.IsWindowVisible(root))
        {
            return null;
        }

        return root;
    }

    public Rectangle? GetBounds(nint window) =>
        User32.GetWindowRect(window, out var rect)
            ? Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom)
            : null;

    public Rectangle GetWorkArea(Point point)
    {
        var screen = ScreenInfo.GetScreenFromPoint(new POINT(point.X, point.Y));
        return Rectangle.FromLTRB(screen.WorkingArea.Left, screen.WorkingArea.Top, screen.WorkingArea.Right, screen.WorkingArea.Bottom);
    }

    public IReadOnlyList<Rectangle> GetOtherWindows(nint target)
    {
        var windows = new List<Rectangle>();

        bool Collect(nint window, nint data)
        {
            _ = data;
            if (window != target && User32.IsWindowVisible(window) && !IsOwnWindow(window) &&
                User32.GetWindowRect(window, out var rect))
            {
                var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    windows.Add(bounds);
                }
            }

            return true;
        }

        _ = User32.EnumWindows(Collect, nint.Zero);
        return windows;
    }

    public bool Move(nint window, Rectangle bounds) => User32.SetWindowPos(
        window,
        nint.Zero,
        bounds.X,
        bounds.Y,
        bounds.Width,
        bounds.Height,
        NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);

    public bool Resize(nint window, Rectangle bounds) => User32.SetWindowPos(
        window,
        nint.Zero,
        bounds.X,
        bounds.Y,
        bounds.Width,
        bounds.Height,
        NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);

    public void Close(nint window) => _ = User32.PostMessage(window, NativeConstants.WM_CLOSE, nint.Zero, nint.Zero);

    public void Minimize(nint window) =>
        _ = User32.SendMessage(window, NativeConstants.WM_SYSCOMMAND, NativeConstants.SC_MINIMIZE, nint.Zero);

    public void Lower(nint window) => _ = User32.SetWindowPos(
        window,
        NativeConstants.HWND_BOTTOM,
        0,
        0,
        0,
        0,
        NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOACTIVATE);

    public void ToggleAlwaysOnTop(nint window)
    {
        var style = User32.GetWindowLongPtr(window, NativeConstants.GWL_EXSTYLE);
        var changed = (style.ToInt64() & NativeConstants.WS_EX_TOPMOST) == 0
            ? style | new IntPtr(NativeConstants.WS_EX_TOPMOST)
            : style & ~new IntPtr(NativeConstants.WS_EX_TOPMOST);
        _ = User32.SetWindowLongPtr(window, NativeConstants.GWL_EXSTYLE, changed);
        _ = User32.SetWindowPos(
            window,
            nint.Zero,
            0,
            0,
            0,
            0,
            NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_FRAMECHANGED);
    }

    public void Center(nint window, Point point)
    {
        if (GetBounds(window) is not { } bounds)
        {
            return;
        }

        var workArea = GetWorkArea(point);
        _ = Move(window, new Rectangle(
            workArea.X + ((workArea.Width - bounds.Width) / 2),
            workArea.Y + ((workArea.Height - bounds.Height) / 2),
            bounds.Width,
            bounds.Height));
    }

    public void ToggleMaximize(nint window, Point point)
    {
        _ = User32.ShowWindow(window, User32.IsZoomed(window) ? NativeConstants.SW_RESTORE : NativeConstants.SW_MAXIMIZE);
        if (DisplayApi.MonitorFromWindow(window, NativeConstants.MONITOR_DEFAULTTONULL) == nint.Zero)
        {
            Center(window, point);
        }
    }

    public void MoveToZone(nint window, WindowResizeZone zone, Point point)
    {
        if (GetBounds(window) is not { } bounds)
        {
            return;
        }

        var workArea = GetWorkArea(point);
        var x = zone.HasFlag(WindowResizeZone.Right) ? workArea.Right - bounds.Width : workArea.Left;
        var y = zone.HasFlag(WindowResizeZone.Bottom) ? workArea.Bottom - bounds.Height : workArea.Top;
        _ = Move(window, new Rectangle(x, y, bounds.Width, bounds.Height));
    }

    public void Focus(nint window) => _ = User32.SetForegroundWindow(window);

    public int GetTransparency(nint window) =>
        User32.GetLayeredWindowAttributes(window, out _, out var alpha, out _) ? alpha : 255;

    public bool SetTransparency(nint window, int alpha)
    {
        var value = byte.CreateChecked(Math.Clamp(alpha, 32, 255));
        if (value == 255)
        {
            var style = User32.GetWindowLongPtr(window, NativeConstants.GWL_EXSTYLE);
            _ = User32.SetWindowLongPtr(
                window,
                NativeConstants.GWL_EXSTYLE,
                style & ~new IntPtr(NativeConstants.WS_EX_LAYERED));
            return User32.SetWindowPos(
                window,
                nint.Zero,
                0,
                0,
                0,
                0,
                NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_FRAMECHANGED);
        }

        var extendedStyle = User32.GetWindowLongPtr(window, NativeConstants.GWL_EXSTYLE);
        _ = User32.SetWindowLongPtr(
            window,
            NativeConstants.GWL_EXSTYLE,
            extendedStyle | new IntPtr(NativeConstants.WS_EX_LAYERED));
        return User32.SetLayeredWindowAttributes(window, 0, value, NativeConstants.LWA_ALPHA);
    }

    private bool IsOwnWindow(nint window) =>
        User32.GetWindowThreadProcessId(window, out var processId) != 0 && processId == currentProcessId;
}
