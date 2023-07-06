﻿using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.App.Utils.Logging;
using System;
using WinUIEx;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Messages;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using Microsoft.UI.Xaml.Navigation;

namespace PhotoViewer.App;

public sealed partial class MainWindow : WindowEx
{
    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    private readonly MainWindowModel viewModel;

    private readonly DialogService dialogService;
    public MainWindow(MainWindowModel viewModel)
    {
        this.viewModel = viewModel;
        this.InitializeComponent();
        AppWindow.Closing += AppWindow_Closing;
        Closed += MainWindow_Closed;

        // AppWindow.SetIcon(); TODO

        dialogService = new DialogService(this);

        viewModel.DialogRequested += ViewModel_DialogRequested;

        viewModel.Subscribe(this, nameof(viewModel.Title), () => Title = viewModel.Title, initialCallback: true);
        viewModel.Subscribe(this, nameof(viewModel.Theme), ApplyTheme, initialCallback: true);

        // TODO
        viewModel.MessengerPublic.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        viewModel.MessengerPublic.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        viewModel.MessengerPublic.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        viewModel.MessengerPublic.Register<NavigateBackMessage>(this, _ => NavigateBack());
           
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
