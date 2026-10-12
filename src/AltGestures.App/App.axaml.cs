using System;
using System.IO;
using AltGestures.App.Services;
using AltGestures.App.ViewModels;
using AltGestures.App.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AltGestures.App;

public class App : Application
{
    private ConfigService? configService;
    private EngineHost? engineHost;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 无头渲染模式只加载 XAML 资源，不碰配置与引擎（Linux 无托盘/钩子）
        var headless = Environment.GetEnvironmentVariable("ALTGESTURES_HEADLESS") == "1";

        configService = new ConfigService(GetConfigPath());

        if (!headless && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(configService)
            };
            desktopLifetime.MainWindow.Show();

            engineHost = new EngineHost(configService);
            engineHost.Error += message => Console.Error.WriteLine($"引擎：{message}");
            engineHost.Start();

            desktopLifetime.ShutdownRequested += (_, _) => DisposeRuntime();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string GetConfigPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AltGestures",
        "config.json");

    private void DisposeRuntime()
    {
        engineHost?.Dispose();
        engineHost = null;
        configService?.Dispose();
        configService = null;
    }
}
