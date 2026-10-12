using AltGestures.App.Services;
using AltGestures.Core.Configuration;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed class GeneralPageViewModel : PageViewModel
{
    public GeneralPageViewModel(ConfigService configService)
        : base(
            configService,
            "通用",
            "启动方式、托盘行为与全局开关。",
            Geometry.Parse("M3.5 7 H16.5 M3.5 13 H16.5 M8 5.7 A1.3 1.3 0 1 0 8 8.3 A1.3 1.3 0 1 0 8 5.7 M13 11.7 A1.3 1.3 0 1 0 13 14.3 A1.3 1.3 0 1 0 13 11.7"))
    {
    }

    public static IReadOnlyList<InterfaceLanguage> LanguageOptions { get; } =
        Enum.GetValues<InterfaceLanguage>();

    public bool StartWithWindows
    {
        get => ConfigService.Config.StartWithWindows;
        set => SetConfig(config => config.StartWithWindows,v => ConfigService.Config.StartWithWindows = v, value);
    }

    public bool HideTrayIconOnStartup
    {
        get => ConfigService.Config.HideTrayIconOnStartup;
        set => SetConfig(
            config => config.HideTrayIconOnStartup,v => ConfigService.Config.HideTrayIconOnStartup = v,
            value);
    }

    public bool StartAsAdministrator
    {
        get => ConfigService.Config.StartAsAdministrator;
        set => SetConfig(
            config => config.StartAsAdministrator,v => ConfigService.Config.StartAsAdministrator = v,
            value);
    }

    public bool ShowTrayIcon
    {
        get => ConfigService.Config.ShowTrayIcon;
        set => SetConfig(config => config.ShowTrayIcon,v => ConfigService.Config.ShowTrayIcon = v, value);
    }

    public InterfaceLanguage Language
    {
        get => ConfigService.Config.InterfaceLanguage;
        set => SetConfig(
            config => config.InterfaceLanguage,v => ConfigService.Config.InterfaceLanguage = v,
            value);
    }

    /// <summary>暂停/恢复热键；留空表示不注册。</summary>
    public string PauseResumeHotKey
    {
        get => ConfigService.Config.PauseResumeHotKey ?? string.Empty;
        set => SetConfig(
            config => config.PauseResumeHotKey ?? string.Empty,
            v => ConfigService.Config.PauseResumeHotKey = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
            value);
    }

    public bool CheckForUpdates
    {
        get => ConfigService.Config.CheckForUpdates;
        set => SetConfig(config => config.CheckForUpdates,v => ConfigService.Config.CheckForUpdates = v, value);
    }

    public bool AllowMultipleInstances
    {
        get => ConfigService.Config.AllowMultipleInstances;
        set => SetConfig(
            config => config.AllowMultipleInstances,v => ConfigService.Config.AllowMultipleInstances = v,
            value);
    }
}
