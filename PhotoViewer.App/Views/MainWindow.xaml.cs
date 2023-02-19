using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.App.Utils.Logging;
using System;
using WinRT.Interop;
using WinUIEx;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using System.ComponentModel;
using PhotoViewer.Core.Messages;

namespace PhotoViewer.App;

public sealed partial class MainWindow : WindowEx
{
    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    public MainWindow(IMessenger messenger)
    {      
        this.InitializeComponent();
        Closed += MainWindow_Closed;
        ViewModelFactory.Initialize(messenger, new DialogService(this));
        messenger.Register<ChangeWindowTitleMessage>(this, msg => Title = msg.NewTitle);
        messenger.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        messenger.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        messenger.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Register<NavigateBackMessage>(this, _ => frame.GoBack());
        messenger.Register<SettingsChangedMessage>(this, msg => OnSettingsChanged(msg.ChangedSetting));
        ApplyTheme(ApplicationSettingsProvider.GetSettings().Theme);
    }

    private void OnSettingsChanged(string? changedSetting)
    {
        if (changedSetting is null || changedSetting == nameof(ApplicationSettings.Theme))
        {
            ApplyTheme(ApplicationSettingsProvider.GetSettings().Theme);
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        var elementTheme = (ElementTheme)theme;

        if (theme == AppTheme.System) 
        {
            // force update of theme
            bool isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;
            frame.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }

        frame.RequestedTheme = elementTheme;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        Log.ArchiveLogFile();
    }

    private void EnterFullscreen()
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private void ExitFullscreen()
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.Default);
    }

    private void NavigateToPage(Type pageModelType, object? parameter)
    {
        frame.Navigate(viewRegistrations.GetViewTypeForViewModelType(pageModelType), parameter);
    }
}
