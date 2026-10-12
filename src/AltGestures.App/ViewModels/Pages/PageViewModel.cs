using System.ComponentModel;
using System.Runtime.CompilerServices;
using AltGestures.Core.Configuration;
using AltGestures.App.Services;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public abstract class PageViewModel : INotifyPropertyChanged
{
    protected PageViewModel(ConfigService configService, string title, string description, Geometry iconGeometry)
    {
        ConfigService = configService;
        Title = title;
        Description = description;
        IconGeometry = iconGeometry;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConfigService ConfigService { get; }

    public string Title { get; }

    public string Description { get; }

    public Geometry IconGeometry { get; }

    protected void SetConfig<T>(Func<AppConfig, T> read, Action<T> write, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(read(ConfigService.Config), value))
        {
            return;
        }

        write(value);
        OnPropertyChanged(propertyName);
        ConfigService.Commit();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
