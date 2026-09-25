using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AltGestures.Core.Windowing;

namespace AltGestures.Core.Configuration;

/// <summary>
/// 负责 JSON 配置文件的读写，不在本阶段提供热重载。
/// </summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string path;

    public ConfigStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        this.path = path;
    }

    static ConfigStore()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }

    public AppConfig Load()
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<AppConfig>(stream, Options) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, config, Options);
    }
}
