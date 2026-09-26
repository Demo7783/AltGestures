using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record WindowActionsPageViewModel : PageViewModel
{
    public WindowActionsPageViewModel()
        : base(
            "窗口操作",
            "按住 Alt 后各鼠标键的行为。不按 Alt 时，这些键全部交给手势。",
            Geometry.Parse("M3 5 H17 V17 H3 Z M7 9 H13 M9 7 V17 M7 17 H3"))
    {
    }
}
