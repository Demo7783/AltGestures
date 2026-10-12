using AltGestures.Core;
using AltGestures.Core.Arbitration;
using AltGestures.Core.Configuration;
using AltGestures.Core.Input;
using AltGestures.Core.Persistence;
using AltGestures.Core.Windowing;
using AltGestures.Core.Windows;

namespace AltGestures.App.Services;

public interface IEngine : IDisposable
{
    bool IsStarted { get; }

    string? Warning { get; }

    void Start();

    void SetGesturesPaused(bool paused);
}

public interface IEngineFactory
{
    IEngine Create(AppConfig config);
}

public sealed class EngineHost : IDisposable
{
    private readonly IEngineFactory engineFactory;
    private readonly object lifetimeGate = new();
    private IEngine? engine;
    private bool shouldRun;
    private bool disposed;

    public EngineHost(ConfigService configService, IEngineFactory? engineFactory = null, string? gestureStorePath = null)
    {
        ConfigService = configService;
        this.engineFactory = engineFactory ?? new CoreEngineFactory(
            gestureStorePath ?? GetDefaultGestureStorePath(configService.Path));
        configService.ConfigChanged += ApplyConfiguration;
    }

    public event Action<string>? Error;

    public ConfigService ConfigService { get; }

    public bool IsRunning { get; private set; }

    public string? LastError { get; private set; }

    public bool GesturesPaused { get; private set; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        shouldRun = true;
        StartEngine();
    }

    public void Stop()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        shouldRun = false;
        StopEngine();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    public void SetGesturesPaused(bool paused)
    {
        GesturesPaused = paused;
        lock (lifetimeGate)
        {
            engine?.SetGesturesPaused(paused);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ConfigService.ConfigChanged -= ApplyConfiguration;
        StopEngine();
        disposed = true;
    }

    private void StartEngine()
    {
        lock (lifetimeGate)
        {
            if (IsRunning)
            {
                return;
            }

            var candidate = engineFactory.Create(ConfigService.Config);
            try
            {
                candidate.Start();
                engine = candidate;
                IsRunning = true;
                LastError = candidate.Warning;
                if (candidate.Warning is not null)
                {
                    Error?.Invoke(candidate.Warning);
                }
            }
            catch (Exception exception)
            {
                candidate.Dispose();
                LastError = $"引擎启动失败：{exception.Message}";
                Error?.Invoke(LastError);
            }
        }
    }

    private void StopEngine()
    {
        lock (lifetimeGate)
        {
            if (!IsRunning)
            {
                return;
            }

            engine?.Dispose();
            engine = null;
            IsRunning = false;
        }
    }

    private void ApplyConfiguration(AppConfig config)
    {
        if (disposed || !shouldRun)
        {
            return;
        }

        if (!config.InputEnabled)
        {
            StopEngine();
            return;
        }

        StopEngine();
        StartEngine();
        if (GesturesPaused && IsRunning)
        {
            engine?.SetGesturesPaused(true);
        }
    }

    private static string GetDefaultGestureStorePath(string configurationPath) =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(configurationPath) ?? ".", "gestures.json");

    private sealed class CoreEngineFactory(string gestureStorePath) : IEngineFactory
    {
        public IEngine Create(AppConfig config) => new CoreEngine(config, gestureStorePath);
    }

    private sealed class CoreEngine : IEngine
    {
        private readonly InputArbiter arbiter;
        private readonly GestureParser parser;
        private readonly PathTracker pathTracker;
        private readonly GlobalHotKeyManager hotKeyManager = new();
        private readonly string? pauseHotKey;
        private bool disposed;

        public CoreEngine(AppConfig config, string gestureStorePath)
        {
            if (!config.TryCreateTriggerModifiers(out _, out var triggerError))
            {
                throw new InvalidOperationException(triggerError);
            }

            var bindings = config.CreateWindowActionBinding();
            var hook = new MouseKeyboardHook();
            var inputSimulator = new InputSimulator();
            pauseHotKey = config.PauseResumeHotKey;
            pathTracker = new PathTracker(hook, inputSimulator);
            var intentStore = new JsonGestureIntentStore(gestureStorePath);
            intentStore.GlobalApp.IsGesturingEnabled = config.GesturesEnabled;
            parser = new GestureParser(pathTracker, new GestureIntentFinder(intentStore, config.DisabledProcesses))
            {
                Enable8DirGesture = config.Enable8DirGesture,
                EnableHotCorners = config.EnableHotCorners,
                EnableRubEdge = config.EnableEdgeRub,
                MaxGestureSteps = config.MaxGestureSteps
            };
            pathTracker.TriggerButton = config.GestureTriggerButton;
            pathTracker.InitialStayTimeout = config.EnableGestureTimeout;
            pathTracker.StayTimeout = config.EnableGestureTimeout;
            pathTracker.InitialStayTimeoutMillis = config.InitialStayTimeoutMillis;
            pathTracker.StayTimeoutMillis = config.InitialStayTimeoutMillis;
            pathTracker.MaxGestureSteps = config.MaxGestureSteps;
            pathTracker.PreferWindowUnderCursorAsTarget = config.PreferWindowUnderCursor;
            pathTracker.DisableInFullscreen = config.DisableInFullscreen;
            arbiter = new InputArbiter(
                hook,
                new WindowBranch(new Win32WindowSystem(), config, inputSimulator),
                bindings,
                config);
        }

        public bool IsStarted { get; private set; }

        public string? Warning { get; private set; }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            parser.Start();
            try
            {
                arbiter.Install();
                IsStarted = true;
            }
            catch
            {
                parser.Dispose();
                throw;
            }

            RegisterPauseHotKey();
        }

        public void SetGesturesPaused(bool paused)
        {
            if (paused)
            {
                parser.Pause();
            }
            else
            {
                parser.Resume();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (IsStarted)
            {
                arbiter.Uninstall();
            }

            hotKeyManager.Dispose();
            parser.Dispose();
            arbiter.Dispose();
            disposed = true;
            IsStarted = false;
        }

        private void RegisterPauseHotKey()
        {
            var text = pauseHotKey;
            if (string.IsNullOrWhiteSpace(text) || !OperatingSystem.IsWindows())
            {
                return;
            }

            if (HotKey.TryParse(text, out var hotKey, out _) &&
                hotKeyManager.RegisterPauseResume(hotKey, parser.TogglePause) != -1)
            {
                return;
            }

            Warning = $"暂停/恢复热键无效或注册失败：{text}";
        }

        private sealed class GestureIntentFinder : AbstractGestureIntentFinder
        {
            private readonly IReadOnlyList<string> disabledProcesses;

            public GestureIntentFinder(IGestureIntentStore store, IReadOnlyList<string> disabledProcesses)
                : base(store)
            {
                this.disabledProcesses = disabledProcesses;
            }

            public override ExeApp? GetExeAppByContext(GestureContext context)
            {
                if (context is not Win32GestureContext windowsContext)
                {
                    return null;
                }

                if (disabledProcesses.Contains(windowsContext.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    return new ExeApp { ExecutablePath = windowsContext.ProcessName, IsGesturingEnabled = false };
                }

                return IntentStore.TryGetExeApp(windowsContext.ProcessName, out var application) ? application : null;
            }
        }
    }
}
