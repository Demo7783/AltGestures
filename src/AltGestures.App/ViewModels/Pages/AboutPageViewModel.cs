using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record AboutPageViewModel : PageViewModel
{
    public AboutPageViewModel()
        : base(
            "关于",
            "版本、许可与项目来源。",
            Geometry.Parse("M10 3 A7 7 0 1 0 17 10 M10 3 A7 7 0 0 1 17 10 M10 3 V17 M3 10 H17"))
    {
    }
}
