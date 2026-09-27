using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record GesturesPageViewModel : PageViewModel
{
    public GesturesPageViewModel()
        : base(
            "鼠标手势",
            "按住触发键画出轨迹，松手执行命令。同一轨迹叠加不同触发键或修饰键可绑不同命令。",
            Geometry.Parse("M3 10 H17 M13 5.5 L17.5 10 L13 14.5"))
    {
    }
}
