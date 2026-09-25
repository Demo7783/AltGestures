using System.Drawing;
using AltGestures.Core.Configuration;
using AltGestures.Core.Input;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 执行点击型、双击型与滚轮型窗口动作。
/// </summary>
public sealed class WindowActionExecutor
{
    private readonly IWindowSystem windowSystem;
    private readonly InputSimulator inputSimulator;

    public WindowActionExecutor(IWindowSystem windowSystem, InputSimulator inputSimulator)
    {
        this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
        this.inputSimulator = inputSimulator ?? throw new ArgumentNullException(nameof(inputSimulator));
    }

    public bool ExecuteClick(WindowAction action, nint window, Point point, bool shiftDown) => action switch
    {
        WindowAction.Close => Invoke(window, () => windowSystem.Close(window)),
        WindowAction.Minimize => Invoke(window, () => windowSystem.Minimize(window)),
        WindowAction.Lower => Invoke(
            window,
            () =>
            {
                if (shiftDown)
                {
                    windowSystem.Minimize(window);
                }
                else
                {
                    windowSystem.Lower(window);
                }
            }),
        WindowAction.AlwaysOnTop => Invoke(window, () => windowSystem.ToggleAlwaysOnTop(window)),
        WindowAction.Center => Invoke(window, () => windowSystem.Center(window, point)),
        _ => false
    };

    public bool ExecuteDoubleClick(WindowMouseButton button, nint window, Point point)
    {
        if (window == nint.Zero)
        {
            return false;
        }

        if (button == WindowMouseButton.Left)
        {
            windowSystem.ToggleMaximize(window, point);
            return true;
        }

        if (button == WindowMouseButton.Right)
        {
            var bounds = windowSystem.GetBounds(window);
            if (bounds is null)
            {
                return false;
            }

            var zone = WindowGeometry.GetResizeZone(bounds.Value, point);
            if (zone == WindowResizeZone.Center)
            {
                zone = WindowResizeZone.Top | WindowResizeZone.Left;
            }

            windowSystem.MoveToZone(window, zone, point);
            return true;
        }

        return false;
    }

    public bool ExecuteWheel(WindowAction action, nint window, int delta, bool shiftDown) => action switch
    {
        WindowAction.AltTab => inputSimulator.Press(
            shiftDown
                ? [VirtualKeyCode.VK_LSHIFT, VirtualKeyCode.VK_LMENU, VirtualKeyCode.VK_TAB]
                : [VirtualKeyCode.VK_LMENU, VirtualKeyCode.VK_TAB]),
        WindowAction.Volume => PressVolume(delta, shiftDown),
        WindowAction.Transparency => window == nint.Zero
            ? false
            : windowSystem.SetTransparency(
                window,
                Math.Clamp(
                    windowSystem.GetTransparency(window) + (delta > 0 ? 16 : -16) / (shiftDown ? 4 : 1),
                    32,
                    255)),
        _ => false
    };

    private static bool Invoke(nint window, Action action)
    {
        if (window == nint.Zero)
        {
            return false;
        }

        action();
        return true;
    }

    private bool PressVolume(int delta, bool shiftDown)
    {
        var key = delta > 0 ? VirtualKeyCode.VK_VOLUME_UP : VirtualKeyCode.VK_VOLUME_DOWN;
        var presses = shiftDown ? 1 : 4;
        var result = true;
        for (var index = 0; index < presses; index++)
        {
            result &= inputSimulator.Press(key);
        }

        return result;
    }
}
