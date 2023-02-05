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

namespace PhotoViewer.App;

public sealed partial class MainWindow : WindowEx
{
    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    private readonly ApplicationSettings settings;

    public MainWindow(IMessenger messenger)
    {      
        this.InitializeComponent();
        Closed += MainWindow_Closed;
        ViewModelFactory.Instance.Initialize(new DialogService(this));
        messenger.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        messenger.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        messenger.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Register<NavigateBackMessage>(this, _ => frame.GoBack());
        settings = ApplicationSettingsProvider.GetSettings();
        settings.PropertyChanged += Settings_PropertyChanged;
        ApplyTheme(settings.Theme);
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ApplicationSettings.Theme))
        {
            ApplyTheme(settings.Theme);
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
