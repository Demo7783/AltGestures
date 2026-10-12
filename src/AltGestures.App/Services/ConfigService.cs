using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using AltGestures.Core.Configuration;

namespace AltGestures.App.Services;

public interface IDebounceScheduler
{
    IDisposable Schedule(Action callback, TimeSpan delay);
}

public sealed class ConfigService : IDisposable
{
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(300);
    private readonly ConfigStore store;
    private readonly IDebounceScheduler scheduler;
    private readonly TimeSpan delay;
    private readonly object saveGate = new();
    private IDisposable? pendingSave;
    private bool disposed;
    private readonly string configurationPath;

    public ConfigService(string path, IDebounceScheduler? scheduler = null, TimeSpan? delay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        configurationPath = path;
        store = new ConfigStore(path);
        this.scheduler = scheduler ?? new TimerScheduler();
        this.delay = delay ?? DefaultDelay;
        try
        {
            Config = store.Load();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            Config = new AppConfig();
            LoadError = exception is FileNotFoundException ? "配置文件不存在，已使用默认配置。" : $"配置文件读取失败，已使用默认配置：{exception.Message}";
            Trace.WriteLine(LoadError);
        }
    }

    private ConfigService()
    {
        configurationPath = "内存配置";
        store = null!;
        scheduler = null!;
        delay = Timeout.InfiniteTimeSpan;
        Config = new AppConfig();
    }

    public static ConfigService CreateInMemory() => new();

    public event Action<AppConfig>? ConfigChanged;

    public event Action<string>? SaveFailed;

    public AppConfig Config { get; }

    public string? LoadError { get; }

    public string? LastSaveError { get; private set; }

    public string Path => configurationPath;

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ConfigChanged?.Invoke(Config);
        ScheduleSave();
    }

    public void Flush()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        IDisposable? save;
        lock (saveGate)
        {
            save = pendingSave;
            pendingSave = null;
        }

        save?.Dispose();
        SaveNow();
    }

    public void SaveNow()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (configurationPath == "内存配置")
        {
            return;
        }

        try
        {
            store.Save(Config);
            LastSaveError = null;
        }
        catch (IOException exception)
        {
            LastSaveError = $"配置保存失败：{exception.Message}";
            SaveFailed?.Invoke(LastSaveError);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (LastSaveError is null)
        {
            Flush();
        }
        else
        {
            DisposePendingSave();
        }

        disposed = true;
    }

    private void ScheduleSave()
    {
        lock (saveGate)
        {
            pendingSave?.Dispose();
            pendingSave = scheduler.Schedule(SaveNow, delay);
        }
    }

    private void DisposePendingSave()
    {
        lock (saveGate)
        {
            pendingSave?.Dispose();
            pendingSave = null;
        }
    }

    private sealed class TimerScheduler : IDebounceScheduler
    {
        public IDisposable Schedule(Action callback, TimeSpan delay) => new Timer(_ => callback(), null, delay, Timeout.InfiniteTimeSpan);
    }
}

