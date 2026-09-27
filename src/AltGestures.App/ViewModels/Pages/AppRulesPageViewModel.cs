using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record AppRulesPageViewModel : PageViewModel
{
    public AppRulesPageViewModel()
        : base(
            "应用规则",
            "为特定程序覆盖全局设置，或把它排除在外。",
            Geometry.Parse("M10 2.5 L16 5 V9.5 Q16 14.5 10 17.5 Q4 14.5 4 9.5 V5 Z"))
    {
    }
}
