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
using Windows.System;
using Microsoft.UI.Xaml.Input;
using PhotoViewer.App.Utils;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using Microsoft.UI.Xaml.Navigation;

namespace PhotoViewer.App;

public sealed partial class MainWindow : WindowEx
{
    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    private readonly MainWindowModel viewModel;

    public MainWindow(IMessenger messenger, ApplicationSettings settings)
    {
        this.InitializeComponent();
        ViewModelFactory.Initialize(messenger, settings, new DialogService(this));
        viewModel = ViewModelFactory.Instance.CreateMainWindowModel();
        AppWindow.Closing += AppWindow_Closing;
        Closed += MainWindow_Closed;
        messenger.Register<ChangeWindowTitleMessage>(this, msg => Title = msg.NewTitle + " - WinUI Photo Viewer");
        messenger.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        messenger.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        messenger.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Register<NavigateBackMessage>(this, _ => NavigateBack());
        messenger.Register<SettingsChangedMessage>(this, msg => OnSettingsChanged(settings, msg.ChangedSetting));
        ApplyTheme(settings.Theme);
        FocusManager.LosingFocus += FocusManager_LosingFocus;
    }

    private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
    {
        // prevent losing focus to the RootScrollViewer (ScrollViewer with parent of type DependencyObject)
        if (e.NewFocusedElement is ScrollViewer sv && sv.Parent.GetType() == typeof(DependencyObject))
        {
            e.TryCancel();
        }
    }

    private void OnSettingsChanged(ApplicationSettings settings, string? changedSetting)
    {
        if (changedSetting is null || changedSetting == nameof(ApplicationSettings.Theme))
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

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            args.Cancel = true;
            await viewModel.OnClosingAsync();
        }
        finally 
        {     
            Close();
        }
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

    private void NavigateBack() 
    {
        frame.GoBack();
    }

    private void Frame_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.F11)
        {
            if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            {
                ExitFullscreen();
            }
            else
            {
                EnterFullscreen();
            }
        }
    }

    private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        Log.Error("Navigation failed", e.Exception);
        e.Handled = true;
    }
}
