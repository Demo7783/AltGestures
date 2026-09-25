using AltGestures.Core.Windowing;

namespace AltGestures.Core.Configuration;

/// <summary>
/// 表示应用程序核心配置。
/// </summary>
public sealed class AppConfig
{
    public IReadOnlyDictionary<string, string> WindowActions { get; set; } = new Dictionary<string, string>
    {
        ["Left"] = nameof(WindowAction.Move),
        ["Middle"] = nameof(WindowAction.Resize),
        ["Right"] = nameof(WindowAction.Resize)
    };

    public int SnapThreshold { get; set; } = 20;

    public int MoveRate { get; set; } = 2;

    public int ResizeRate { get; set; } = 5;

    public bool AutoFocus { get; set; }

    public bool Aero { get; set; } = true;

    public WindowActionBinding CreateWindowActionBinding() => WindowActionBinding.FromNames(WindowActions);
}
