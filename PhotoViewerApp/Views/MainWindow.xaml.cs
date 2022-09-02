﻿using Microsoft.UI.Xaml;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using System;

namespace PhotoViewerApp;

public sealed partial class MainWindow : Window
{
    private readonly ViewRegistrations viewRegistrations = new ViewRegistrations();

    public IDialogService DialogService { get; }

    public MainWindow(IMessenger messenger)
    {
        this.InitializeComponent();
        DialogService = new DialogService(this);
        messenger.Subscribe<EnterFullscreenMessage>(msg => EnterFullscreen());
        messenger.Subscribe<ExitFullscreenMessage>(msg => ExitFullscreen());
        messenger.Subscribe<NavigateToPageMessage>(msg => NavigateToPage(msg.PageType, msg.Parameter));
    }

    private void EnterFullscreen()
    {
        var appWindow = GetAppWindow();
        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private void ExitFullscreen()
    {
        var appWindow = GetAppWindow();
        appWindow.SetPresenter(AppWindowPresenterKind.Default);
    }

    private void NavigateToPage(Type pageModelType, object? parameter)
    {
        frame.Navigate(viewRegistrations.ViewTypeByViewModelType[pageModelType], parameter);
    }

    private AppWindow GetAppWindow() 
    {
        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        return AppWindow.GetFromWindowId(windowId);
    }
}
