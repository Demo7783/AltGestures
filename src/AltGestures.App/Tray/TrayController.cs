using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AltGestures.App.Services;

namespace AltGestures.App.Tray;

public sealed class TrayController : IDisposable
{
    private readonly EngineHost engineHost;
    private readonly ConfigService configService;
    private readonly Action openSettings;
    private readonly Action exit;
    private readonly NativeMenuItem enableItem = new()
    {
        Header = "启用",
        ToggleType = MenuItemToggleType.CheckBox
    };
    private readonly NativeMenuItem pauseGesturesItem = new()
    {
        Header = "暂停手势",
        ToggleType = MenuItemToggleType.CheckBox
    };
    private RenderTargetBitmap? iconBitmap;
    private TrayIcon? trayIcon;
    private bool disposed;

    public TrayController(EngineHost engineHost, ConfigService configService, Action openSettings, Action exit)
    {
        this.engineHost = engineHost;
        this.configService = configService;
        this.openSettings = openSettings;
        this.exit = exit;
        enableItem.Click += (_, _) =>
        {
            enableItem.IsChecked = enableItem.IsChecked != true;
            engineHost.SetInputEnabled(enableItem.IsChecked == true);
        };
        pauseGesturesItem.Click += (_, _) =>
        {
            pauseGesturesItem.IsChecked = pauseGesturesItem.IsChecked != true;
            engineHost.SetGesturesPaused(pauseGesturesItem.IsChecked == true);
        };
        configService.ConfigChanged += Configure;
    }

    public bool IsInitialized { get; private set; }

    public bool Initialize()
    {
        if (Environment.GetEnvironmentVariable("ALTGESTURES_HEADLESS") == "1")
        {
            return false;
        }

        try
        {
            iconBitmap = new RenderTargetBitmap(new PixelSize(32, 32), new Vector(96, 96));
            using var context = iconBitmap.CreateDrawingContext();
            var pen = new Pen(Brush.Parse("#FF2C2C2C"), 2.2, null, PenLineCap.Round, PenLineJoin.Round);
            context.DrawLine(pen, new Point(7, 25), new Point(16, 7));
            context.DrawLine(pen, new Point(16, 7), new Point(25, 25));
            context.DrawLine(pen, new Point(11, 18), new Point(21, 18));

            var menu = new NativeMenu();
            menu.Items.Add(enableItem);
            menu.Items.Add(pauseGesturesItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            var openSettingsItem = new NativeMenuItem { Header = "打开设置" };
            openSettingsItem.Click += (_, _) => openSettings();
            menu.Items.Add(openSettingsItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            var exitItem = new NativeMenuItem { Header = "退出" };
            exitItem.Click += (_, _) => exit();
            menu.Items.Add(exitItem);

            trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(iconBitmap),
                Menu = menu,
                ToolTipText = "AltGestures"
            };
            Configure(configService.Config);
            if (Application.Current is { } app)
            {
                TrayIcon.SetIcons(app, new TrayIcons { trayIcon });
            }
            IsInitialized = true;
            return true;
        }
        catch (Exception)
        {
            Dispose();
            return false;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        configService.ConfigChanged -= Configure;
        trayIcon?.Dispose();
        trayIcon = null;
        iconBitmap?.Dispose();
        iconBitmap = null;
        IsInitialized = false;
        disposed = true;
    }

    private void Configure(AltGestures.Core.Configuration.AppConfig config)
    {
        enableItem.IsChecked = config.InputEnabled;
        if (trayIcon is not null)
        {
            trayIcon.IsVisible = config.ShowTrayIcon;
        }
    }
}
