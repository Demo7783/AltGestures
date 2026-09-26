using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public abstract record PageViewModel(string Title, string Description, Geometry IconGeometry);
