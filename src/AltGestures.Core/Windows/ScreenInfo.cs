using AltGestures.Core.Interop;

namespace AltGestures.Core.Windows;

public sealed class ScreenInfo
{
    public ScreenInfo(bool isPrimary, RECT bounds, RECT workingArea)
    {
        IsPrimary = isPrimary;
        Bounds = bounds;
        WorkingArea = workingArea;
    }

    public bool IsPrimary { get; }

    public RECT Bounds { get; }

    public RECT WorkingArea { get; }

    public static IReadOnlyList<ScreenInfo> GetAllScreens()
    {
        var screens = new List<ScreenInfo>();

        bool Enumerate(nint monitor, nint deviceContext, ref RECT rect, nint data)
        {
            screens.Add(FromMonitor(monitor));
            return true;
        }

        if (!DisplayApi.EnumDisplayMonitors(nint.Zero, nint.Zero, Enumerate, nint.Zero))
        {
            ThrowLastWin32Error(nameof(DisplayApi.EnumDisplayMonitors));
        }

        return screens;
    }

    public static ScreenInfo GetPrimaryScreen()
    {
        var primary = GetAllScreens().FirstOrDefault(screen => screen.IsPrimary);
        return primary ?? throw new InvalidOperationException("未找到主显示器。");
    }

    public static ScreenInfo GetScreenFromPoint(POINT point)
    {
        var monitor = DisplayApi.MonitorFromPoint(point, NativeConstants.MONITOR_DEFAULTTONEAREST);
        if (monitor == nint.Zero)
        {
            ThrowLastWin32Error(nameof(DisplayApi.MonitorFromPoint));
        }

        return FromMonitor(monitor);
    }

    public static RECT GetPrimaryWorkArea()
    {
        var workArea = default(RECT);
        if (!User32.SystemParametersInfo(
                NativeConstants.SPI_GETWORKAREA,
                0,
                ref workArea,
                0))
        {
            ThrowLastWin32Error(nameof(User32.SystemParametersInfo));
        }

        return workArea;
    }

    private static ScreenInfo FromMonitor(nint monitor)
    {
        var information = MONITORINFO.Create();
        if (!DisplayApi.GetMonitorInfo(monitor, ref information))
        {
            ThrowLastWin32Error(nameof(DisplayApi.GetMonitorInfo));
        }

        return new ScreenInfo(
            (information.Flags & NativeConstants.MONITORINFOF_PRIMARY) != 0,
            information.Monitor,
            information.Work);
    }

    private static void ThrowLastWin32Error(string methodName) =>
        throw new InvalidOperationException($"{methodName} 调用失败，Win32 错误码 {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}。");
}
