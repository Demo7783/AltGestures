using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record TriggerFeedbackPageViewModel : PageViewModel
{
    public TriggerFeedbackPageViewModel()
        : base(
            "触发与反馈",
            "控制手势怎么被触发，以及画的时候屏幕上显示什么。",
            Geometry.Parse("M10 3 A7 7 0 1 0 17 10 M10 6 A4 4 0 1 0 14 10"))
    {
    }
}
