using AltGestures.App.Services;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed class AppRulesPageViewModel : PageViewModel
{
    public AppRulesPageViewModel(ConfigService configService)
        : base(
            configService,
            "应用规则",
            "为特定程序覆盖全局设置，或把它排除在外。",
            Geometry.Parse("M10 2.5 L16 5 V9.5 Q16 14.5 10 17.5 Q4 14.5 4 9.5 V5 Z"))
    {
    }

    /// <summary>手势在这些程序里禁用；每行一个进程名。</summary>
    public string DisabledProcessesText
    {
        get => string.Join(Environment.NewLine, ConfigService.Config.DisabledProcesses);
        set => SetConfig(
            config => string.Join(Environment.NewLine, config.DisabledProcesses),
            v => ConfigService.Config.DisabledProcesses = SplitLines(v),
            value);
    }

    /// <summary>全局忽略的窗口类名；每行一个。</summary>
    public string GlobalBlacklistText
    {
        get => string.Join(Environment.NewLine, ConfigService.Config.GlobalBlacklist);
        set => SetConfig(
            config => string.Join(Environment.NewLine, config.GlobalBlacklist),
            v => ConfigService.Config.GlobalBlacklist = SplitLines(v),
            value);
    }

    private static IReadOnlyList<string> SplitLines(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
