using System.Reflection;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed record AboutPageViewModel : PageViewModel
{
    public AboutPageViewModel()
        : base(
            "关于",
            "版本、许可与项目来源。",
            Geometry.Parse("M10 2.5 A7.5 7.5 0 1 0 10 17.5 A7.5 7.5 0 1 0 10 2.5 M10 9 V14 M10 6.2 V6.3"))
    {
        Version = ResolveVersion();
    }

    /// <summary>程序集版本号，取自 csproj 的 &lt;Version&gt;，不硬编码。</summary>
    public string Version { get; }

    private static string ResolveVersion()
    {
        var assembly = typeof(AboutPageViewModel).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = informational ?? assembly.GetName().Version?.ToString(3) ?? "未知版本";

        // 去掉 SourceLink 可能追加的 +commit 后缀
        var plusIndex = version.IndexOf('+');
        var clean = plusIndex >= 0 ? version[..plusIndex] : version;
        return $"{clean}（开发中）";
    }
}
