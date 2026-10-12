using AltGestures.App.Services;
using AltGestures.Core.Configuration;
using AltGestures.Core.Windowing;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed class WindowActionsPageViewModel : PageViewModel
{
    public WindowActionsPageViewModel(ConfigService configService)
        : base(
            configService,
            "窗口操作",
            "按住 Alt 后各鼠标键的行为。不按 Alt 时，这些键全部交给手势。",
            Geometry.Parse("M2.5 4.5 H17.5 V15.5 H2.5 Z M2.5 8 H17.5"))
    {
    }

    public static IReadOnlyList<WindowAction> ActionOptions { get; } =
        Enum.GetValues<WindowAction>();

    public WindowAction LeftAction
    {
        get => ReadAction("Left", WindowAction.Move);
        set => WriteAction("Left", value);
    }

    public WindowAction MiddleAction
    {
        get => ReadAction("Middle", WindowAction.Resize);
        set => WriteAction("Middle", value);
    }

    public WindowAction RightAction
    {
        get => ReadAction("Right", WindowAction.Resize);
        set => WriteAction("Right", value);
    }

    public WindowAction WheelAction
    {
        get => ReadAction("Wheel", WindowAction.None);
        set => WriteAction("Wheel", value);
    }

    public WindowAction XButton1Action
    {
        get => ReadAction("X1", WindowAction.None);
        set => WriteAction("X1", value);
    }

    public WindowAction XButton2Action
    {
        get => ReadAction("X2", WindowAction.None);
        set => WriteAction("X2", value);
    }

    public bool AutoFocus
    {
        get => ConfigService.Config.AutoFocus;
        set => SetConfig(config => config.AutoFocus,v => ConfigService.Config.AutoFocus = v, value);
    }

    public bool Aero
    {
        get => ConfigService.Config.Aero;
        set => SetConfig(config => config.Aero,v => ConfigService.Config.Aero = v, value);
    }

    public int SnapThreshold
    {
        get => ConfigService.Config.SnapThreshold;
        set => SetPositive(config => config.SnapThreshold,v => ConfigService.Config.SnapThreshold = v, value);
    }

    public int MoveRate
    {
        get => ConfigService.Config.MoveRate;
        set => SetPositive(config => config.MoveRate,v => ConfigService.Config.MoveRate = v, value);
    }

    public int ResizeRate
    {
        get => ConfigService.Config.ResizeRate;
        set => SetPositive(config => config.ResizeRate,v => ConfigService.Config.ResizeRate = v, value);
    }

    private WindowAction ReadAction(string key, WindowAction fallback) =>
        ConfigService.Config.WindowActions.TryGetValue(key, out var name) &&
        Enum.TryParse(name, true, out WindowAction action)
            ? action
            : fallback;

    private void WriteAction(string key, WindowAction action) =>
        SetConfig(
            config => ReadAction(key, WindowAction.None),
            _ => ConfigService.Config.WindowActions = new Dictionary<string, string>(ConfigService.Config.WindowActions)
            {
                [key] = action.ToString()
            },
            action);

    private void SetPositive(
        Func<AppConfig, int> read,
        Action<int> write,
        int value)
    {
        if (value <= 0)
        {
            OnPropertyChanged();
            return;
        }

        SetConfig(read, write, value);
    }
}
