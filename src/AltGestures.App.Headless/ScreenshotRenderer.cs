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

    /// <summary>期望渲染的页面数。不足即视为失败，不替换成品目录。</summary>
    private const int ExpectedPageCount = 6;

    public static int Main(string[] args)
    {
        Console.WriteLine("初始化无头渲染环境...");
        Environment.SetEnvironmentVariable("ALTGESTURES_HEADLESS", "1");
        if (args.Length != 0)
        {
            Console.Error.WriteLine("用法：dotnet run --project src/AltGestures.App.Headless");
            return 2;
        }

        var root = FindRepositoryRoot();
        var stagingDirectory = PrepareStagingDirectory(root);
        List<ScreenshotResult> screenshots = [];

        var appBuilder = BuildAvaloniaApp();
        appBuilder.AfterSetup(_ => Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine("开始渲染六页...");
            screenshots.AddRange(RenderAll(stagingDirectory));
            Console.WriteLine("六页渲染完成。");

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }));

        try
        {
            appBuilder.StartWithClassicDesktopLifetime([]);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"渲染失败，已保留原有截图：{ex.Message}");
            return 1;
        }

        Console.WriteLine("Headless 渲染完成。");

        if (!TryCommit(stagingDirectory, root, screenshots.Count))
        {
            return 1;
        }

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

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("未找到仓库根目录。");
    }

    /// <summary>渲染先落在暂存目录：中途失败时成品截图原封不动。</summary>
    private static DirectoryInfo PrepareStagingDirectory(DirectoryInfo root)
    {
        var staging = new DirectoryInfo(Path.Combine(root.FullName, "artifacts", ".screenshots-staging"));
        if (staging.Exists)
        {
            staging.Delete(recursive: true);
        }

        return Directory.CreateDirectory(staging.FullName);
    }

    /// <summary>六页全部成功后才替换成品目录；任何一页缺失都整体放弃并保留旧图。</summary>
    private static bool TryCommit(DirectoryInfo staging, DirectoryInfo root, int renderedCount)
    {
        if (renderedCount != ExpectedPageCount)
        {
            Console.Error.WriteLine($"只渲染成功 {renderedCount}/{ExpectedPageCount} 页，已保留原有截图。");
            return false;
        }

        var final = Directory.CreateDirectory(Path.Combine(root.FullName, "artifacts", "screenshots"));
        foreach (var file in final.GetFiles("*.png"))
        {
            file.Delete();
        }

        foreach (var file in staging.GetFiles("*.png"))
        {
            file.MoveTo(Path.Combine(final.FullName, file.Name), overwrite: true);
        }

        staging.Delete(recursive: true);
        Console.WriteLine($"已提交 {renderedCount} 张截图。");
        return true;
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
