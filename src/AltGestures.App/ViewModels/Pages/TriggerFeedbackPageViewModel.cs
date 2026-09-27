using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record TriggerFeedbackPageViewModel : PageViewModel
{
    public TriggerFeedbackPageViewModel()
        : base(
            "触发与反馈",
            "控制手势怎么被触发、以及画的时候屏幕上显示什么。",
            Geometry.Parse("M11.5 2.5 L5.5 11 H10 L9 17.5 L15 9 H10.5 Z"))
    {
    }
}
