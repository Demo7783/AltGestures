using System.Collections.ObjectModel;
using AltGestures.App.ViewModels.Pages;

namespace AltGestures.App.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel()
    {
        Pages =
        [
            new WindowActionsPageViewModel(),
            new GesturesPageViewModel(),
            new TriggerFeedbackPageViewModel(),
            new AppRulesPageViewModel(),
            new GeneralPageViewModel(),
            new AboutPageViewModel()
        ];
        SelectedPage = Pages[0];
    }

    public ObservableCollection<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage { get; set; }
}
