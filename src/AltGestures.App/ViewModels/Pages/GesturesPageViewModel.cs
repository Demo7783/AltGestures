using AltGestures.App.Services;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed class GesturesPageViewModel : PageViewModel
{
    public GesturesPageViewModel(ConfigService configService)
        : base(
            configService,
            "鼠标手势",
            "按住触发键画出轨迹，松手执行命令。同一轨迹叠加不同触发键或修饰键可绑不同命令。",
            Geometry.Parse("M3 10 H17 M13 5.5 L17.5 10 L13 14.5"))
    {
    }

    public bool GesturesEnabled
    {
        get => ConfigService.Config.GesturesEnabled;
        set => SetConfig(config => config.GesturesEnabled,v => ConfigService.Config.GesturesEnabled = v, value);
    }

    public bool InputEnabled
    {
        get => ConfigService.Config.InputEnabled;
        set => SetConfig(config => config.InputEnabled,v => ConfigService.Config.InputEnabled = v, value);
    }
}
