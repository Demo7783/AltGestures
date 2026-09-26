using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record GeneralPageViewModel : PageViewModel
{
    public GeneralPageViewModel()
        : base(
            "通用",
            "启动方式、托盘行为与全局开关。",
            Geometry.Parse("M5 5 H15 V17 H5 Z M8 8 H12 M8 12 H12 M8 16 H11"))
    {
    }
}
