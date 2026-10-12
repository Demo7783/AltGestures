using AltGestures.Core.Input;
using AltGestures.Core.Windowing;

namespace AltGestures.Core.Configuration;

public enum InterfaceLanguage
{
    Chinese,
    English
}

/// <summary>
/// 表示应用程序核心配置。
/// </summary>
public sealed class AppConfig
{
    public bool InputEnabled { get; set; } = true;

    public bool GesturesEnabled { get; set; } = true;

    public GestureTriggerButton GestureTriggerButton { get; set; } = GestureTriggerButton.Right;

    public bool ShowGestureTrail { get; set; } = true;

    public bool FadeGestureTrail { get; set; } = true;

    public bool ShowCommandName { get; set; } = true;

    public int InitialStayTimeoutMillis { get; set; } = 200;

    public bool Enable8DirGesture { get; set; }

    public bool EnableGestureTimeout { get; set; } = true;

    public int MaxGestureSteps { get; set; } = 4;

    public bool PreferWindowUnderCursor { get; set; } = true;

    public bool DisableInFullscreen { get; set; }

    public bool EnableHotCorners { get; set; } = true;

    public bool EnableEdgeRub { get; set; } = true;

    public IReadOnlyList<string> DisabledProcesses { get; set; } = [];

    public IReadOnlyList<string> GlobalBlacklist { get; set; } =
        ["TaskSwitcherWnd", "TaskSwitcherOverlayWnd", "#32770"];

    public bool StartWithWindows { get; set; } = true;

    public bool HideTrayIconOnStartup { get; set; }

    public bool StartAsAdministrator { get; set; }

    public bool ShowTrayIcon { get; set; } = true;

    public InterfaceLanguage InterfaceLanguage { get; set; } = InterfaceLanguage.Chinese;

    public bool CheckForUpdates { get; set; } = true;

    public bool AllowMultipleInstances { get; set; }

    public IReadOnlyDictionary<string, string> WindowActions { get; set; } = new Dictionary<string, string>
    {
        ["Left"] = nameof(WindowAction.Move),
        ["Middle"] = nameof(WindowAction.Resize),
        ["Right"] = nameof(WindowAction.Resize)
    };

    public int SnapThreshold { get; set; } = 20;

    public int MoveRate { get; set; } = 2;

    public int ResizeRate { get; set; } = 5;

    /// <summary>
    /// 获取或设置窗口操作触发键；每个名称代表左右两侧。
    /// </summary>
    public IReadOnlyList<string> TriggerKeys { get; set; } = ["Alt"];

    /// <summary>
    /// 获取或设置暂停/恢复热键；空值表示清空。
    /// </summary>
    public string? PauseResumeHotKey { get; set; } = "Ctrl+Alt+P";

    public bool AutoFocus { get; set; }

    public bool Aero { get; set; } = true;

    public WindowActionBinding CreateWindowActionBinding() => WindowActionBinding.FromNames(WindowActions);

    public bool TryCreateTriggerModifiers(
        out HotKeyModifiers triggerKeys,
        out string? error) => HotKey.TryParseTriggerKeys(TriggerKeys, out triggerKeys, out error);
}
