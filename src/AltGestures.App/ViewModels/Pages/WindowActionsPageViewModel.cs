using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record WindowActionsPageViewModel : PageViewModel
{
    public WindowActionsPageViewModel()
        : base(
            "窗口操作",
            "按住 Alt 后各鼠标键的行为。不按 Alt 时，这些键全部交给手势。",
            Geometry.Parse("M2.5 4.5 H17.5 V15.5 H2.5 Z M2.5 8 H17.5"))
    {
    }
}
