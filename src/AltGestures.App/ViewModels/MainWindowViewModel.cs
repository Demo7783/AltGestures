using System.Collections.ObjectModel;
using AltGestures.App.Services;
using AltGestures.App.ViewModels.Pages;

namespace AltGestures.App.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(ConfigService configService)
    {
        Pages =
        [
            new WindowActionsPageViewModel(configService),
            new GesturesPageViewModel(configService),
            new TriggerFeedbackPageViewModel(configService),
            new AppRulesPageViewModel(configService),
            new GeneralPageViewModel(configService),
            new AboutPageViewModel(configService)
        ];
        SelectedPage = Pages[0];
    }

    public ObservableCollection<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage { get; set; }
}
