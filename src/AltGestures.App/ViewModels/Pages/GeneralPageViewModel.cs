using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record GeneralPageViewModel : PageViewModel
{
    public GeneralPageViewModel()
        : base(
            "通用",
            "启动方式、托盘行为与全局开关。",
            Geometry.Parse("M3.5 7 H16.5 M3.5 13 H16.5 M8 5.7 A1.3 1.3 0 1 0 8 8.3 A1.3 1.3 0 1 0 8 5.7 M13 11.7 A1.3 1.3 0 1 0 13 14.3 A1.3 1.3 0 1 0 13 11.7"))
    {
    }
}
