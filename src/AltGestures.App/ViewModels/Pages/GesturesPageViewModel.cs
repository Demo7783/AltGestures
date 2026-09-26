using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record GesturesPageViewModel : PageViewModel
{
    public GesturesPageViewModel()
        : base(
            "鼠标手势",
            "紧凑表格：轨迹、触发键、命令与参数。",
            Geometry.Parse("M3 10 L17 10 M12 5 L17 10 L12 15"))
    {
    }
}
