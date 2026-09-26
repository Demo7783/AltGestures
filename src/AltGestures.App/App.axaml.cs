using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AltGestures.App.Views;

namespace AltGestures.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (Environment.GetEnvironmentVariable("ALTGESTURES_HEADLESS") != "1" &&
            ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.MainWindow = new MainWindow();
            desktopLifetime.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
