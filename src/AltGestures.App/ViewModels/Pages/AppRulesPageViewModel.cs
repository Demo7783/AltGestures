using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record AppRulesPageViewModel : PageViewModel
{
    public AppRulesPageViewModel()
        : base(
            "应用规则",
            "为特定程序覆盖全局设置，或把它排除在外。",
            Geometry.Parse("M4 16 L6 8 H14 L16 16 M7 16 H13 M8 5 H12"))
    {
    }
}
