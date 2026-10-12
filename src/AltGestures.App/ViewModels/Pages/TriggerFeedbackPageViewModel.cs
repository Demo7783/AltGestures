using AltGestures.App.Services;
using AltGestures.Core;
using AltGestures.Core.Configuration;
using Avalonia.Media;

namespace AltGestures.App.ViewModels.Pages;

public sealed class TriggerFeedbackPageViewModel : PageViewModel
{
    public TriggerFeedbackPageViewModel(ConfigService configService)
        : base(
            configService,
            "触发与反馈",
            "控制手势怎么被触发、以及画的时候屏幕上显示什么。",
            Geometry.Parse("M11.5 2.5 L5.5 11 H10 L9 17.5 L15 9 H10.5 Z"))
    {
    }

    public static IReadOnlyList<GestureTriggerButton> TriggerButtonOptions { get; } =
    [
        GestureTriggerButton.Right,
        GestureTriggerButton.Middle,
        GestureTriggerButton.X1,
        GestureTriggerButton.X2
    ];

    public GestureTriggerButton TriggerButton
    {
        get => ConfigService.Config.GestureTriggerButton;
        set => SetConfig(
            config => config.GestureTriggerButton,v => ConfigService.Config.GestureTriggerButton = v,
            value);
    }

    public bool ShowGestureTrail
    {
        get => ConfigService.Config.ShowGestureTrail;
        set => SetConfig(config => config.ShowGestureTrail,v => ConfigService.Config.ShowGestureTrail = v, value);
    }

    public bool FadeGestureTrail
    {
        get => ConfigService.Config.FadeGestureTrail;
        set => SetConfig(config => config.FadeGestureTrail,v => ConfigService.Config.FadeGestureTrail = v, value);
    }

    public bool ShowCommandName
    {
        get => ConfigService.Config.ShowCommandName;
        set => SetConfig(config => config.ShowCommandName,v => ConfigService.Config.ShowCommandName = v, value);
    }

    public int InitialStayTimeoutMillis
    {
        get => ConfigService.Config.InitialStayTimeoutMillis;
        set => SetPositive(
            config => config.InitialStayTimeoutMillis,v => ConfigService.Config.InitialStayTimeoutMillis = v,
            value);
    }

    public bool EnableGestureTimeout
    {
        get => ConfigService.Config.EnableGestureTimeout;
        set => SetConfig(config => config.EnableGestureTimeout,v => ConfigService.Config.EnableGestureTimeout = v, value);
    }

    public bool Enable8DirGesture
    {
        get => ConfigService.Config.Enable8DirGesture;
        set => SetConfig(config => config.Enable8DirGesture,v => ConfigService.Config.Enable8DirGesture = v, value);
    }

    public int MaxGestureSteps
    {
        get => ConfigService.Config.MaxGestureSteps;
        set => SetPositive(config => config.MaxGestureSteps,v => ConfigService.Config.MaxGestureSteps = v, value);
    }

    public bool PreferWindowUnderCursor
    {
        get => ConfigService.Config.PreferWindowUnderCursor;
        set => SetConfig(
            config => config.PreferWindowUnderCursor,v => ConfigService.Config.PreferWindowUnderCursor = v,
            value);
    }

    public bool DisableInFullscreen
    {
        get => ConfigService.Config.DisableInFullscreen;
        set => SetConfig(config => config.DisableInFullscreen,v => ConfigService.Config.DisableInFullscreen = v, value);
    }

    public bool EnableHotCorners
    {
        get => ConfigService.Config.EnableHotCorners;
        set => SetConfig(config => config.EnableHotCorners,v => ConfigService.Config.EnableHotCorners = v, value);
    }

    public bool EnableEdgeRub
    {
        get => ConfigService.Config.EnableEdgeRub;
        set => SetConfig(config => config.EnableEdgeRub,v => ConfigService.Config.EnableEdgeRub = v, value);
    }

    private void SetPositive(Func<AppConfig, int> read, Action<int> write, int value)
    {
        if (value <= 0)
        {
            OnPropertyChanged();
            return;
        }

        SetConfig(read, write, value);
    }
}
