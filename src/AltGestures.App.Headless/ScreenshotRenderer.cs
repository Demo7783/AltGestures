using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using AltGestures.App.ViewModels;
using AltGestures.App.ViewModels.Pages;
using AltGestures.App.Views;

namespace AltGestures.App.Headless;

internal static class ScreenshotRenderer
{
    private const int Width = 960;
    private const int Height = 620;

    public static int Main(string[] args)
    {
        Console.WriteLine("初始化无头渲染环境...");
        Environment.SetEnvironmentVariable("ALTGESTURES_HEADLESS", "1");
        if (args.Length != 0)
        {
            Console.Error.WriteLine("用法：dotnet run --project src/AltGestures.App.Headless");
            return 2;
        }

        var outputDirectory = CreateOutputDirectory();
        List<ScreenshotResult> screenshots = [];
        var appBuilder = BuildAvaloniaApp();
        appBuilder.AfterSetup(_ => Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine("开始渲染六页...");
            screenshots.AddRange(RenderAll(outputDirectory));
            Console.WriteLine("六页渲染完成。");

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }));
        appBuilder.StartWithClassicDesktopLifetime([]);

        Console.WriteLine("Headless 渲染完成。");

        foreach (var screenshot in screenshots)
        {
            Console.WriteLine($"{screenshot.FileName}\t{screenshot.Width}x{screenshot.Height}\t{screenshot.Bytes}");
        }

        return 0;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        Console.WriteLine("构建 Avalonia Headless AppBuilder...");
        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia();
    }

    private static DirectoryInfo CreateOutputDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("未找到仓库根目录。");
        }

        var screenshots = Directory.CreateDirectory(Path.Combine(directory.FullName, "artifacts", "screenshots"));
        foreach (var file in screenshots.GetFiles("*.png"))
        {
            file.Delete();
        }

        return screenshots;
    }

    private static IReadOnlyList<ScreenshotResult> RenderAll(DirectoryInfo outputDirectory)
    {
        var window = new MainWindow
        {
            Width = Width,
            Height = Height
        };
        window.Show();

        return
        [
            Render(window, outputDirectory, "01-window-actions.png", page => page is WindowActionsPageViewModel),
            Render(window, outputDirectory, "02-gestures.png", page => page is GesturesPageViewModel),
            Render(window, outputDirectory, "03-trigger-feedback.png", page => page is TriggerFeedbackPageViewModel),
            Render(window, outputDirectory, "04-app-rules.png", page => page is AppRulesPageViewModel),
            Render(window, outputDirectory, "05-general.png", page => page is GeneralPageViewModel),
            Render(window, outputDirectory, "06-about.png", page => page is AboutPageViewModel)
        ];
    }

    private static ScreenshotResult Render(
        MainWindow window,
        DirectoryInfo outputDirectory,
        string fileName,
        Func<PageViewModel, bool> pageSelector)
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SelectedPage = viewModel.Pages.Single(pageSelector);

        window.DataContext = viewModel;
        window.UpdateLayout();

        using var bitmap = new RenderTargetBitmap(
            new PixelSize(Width, Height),
            new Vector(96, 96));
        bitmap.Render(window);

        var path = Path.Combine(outputDirectory.FullName, fileName);
        bitmap.Save(path, PngBitmapEncoderOptions.Default);

        var bytes = new FileInfo(path).Length;
        if (bytes <= 10 * 1024)
        {
            throw new InvalidOperationException($"{fileName} 只有 {bytes} 字节，疑似空白渲染。");
        }

        return new ScreenshotResult(fileName, Width, Height, bytes);
    }

    private sealed record ScreenshotResult(string FileName, int Width, int Height, long Bytes);
}
