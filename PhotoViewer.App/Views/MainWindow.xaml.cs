using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using Windows.Foundation;
using Windows.System;
using WinUIEx;

namespace PhotoViewer.App;

public sealed partial class MainWindow : WindowEx
{
    public event TypedEventHandler<MainWindow, AppWindowClosingEventArgs>? Closing;

    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    private readonly MainWindowModel viewModel;

    private readonly DialogService dialogService;

    private readonly IMessenger messenger;

    public MainWindow(MainWindowModel viewModel, IMessenger messenger)
    {
        this.viewModel = viewModel;
        this.messenger = messenger;
        this.InitializeComponent();
        AppWindow.Closing += AppWindow_Closing;
        Closed += MainWindow_Closed;

        AppWindow.SetIcon("Assets/icon.ico");

        dialogService = new DialogService(this);

        viewModel.DialogRequested += ViewModel_DialogRequested;

        viewModel.Subscribe(this, nameof(viewModel.Title), () => Title = viewModel.Title, initialCallback: true);
        viewModel.Subscribe(this, nameof(viewModel.Theme), ApplyTheme, initialCallback: true);

        messenger.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        messenger.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        messenger.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Register<NavigateBackMessage>(this, _ => NavigateBack());

        FocusManager.LosingFocus += FocusManager_LosingFocus;
    }

    private void ViewModel_DialogRequested(object? sender, DialogRequestedEventArgs e)
    {
        e.AddTask(dialogService.ShowDialogAsync(e.DialogModel));
    }

    private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
    {
        // prevent losing focus to the RootScrollViewer (ScrollViewer with parent of type DependencyObject)
        if (e.NewFocusedElement is ScrollViewer sv && sv.Parent.GetType() == typeof(DependencyObject))
        {
            e.TryCancel();
        }
    }

    private void ApplyTheme()
    {
        var elementTheme = (ElementTheme)viewModel.Theme;

        if (viewModel.Theme == AppTheme.System)
        {
            // force update of theme
            bool isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;
            frame.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }

        frame.RequestedTheme = elementTheme;
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        Closing?.Invoke(this, args);

        if (args.Cancel)
        {
            args.Cancel = true;
            return;
        }

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
        messenger.Send(new AppClosingMessage());
        messenger.UnregisterAll(this);
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
