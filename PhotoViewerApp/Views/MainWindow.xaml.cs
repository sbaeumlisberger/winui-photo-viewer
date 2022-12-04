using Microsoft.UI;
using Microsoft.UI.Windowing;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using System;
using WinRT.Interop;
using WinUIEx;

namespace PhotoViewerApp;

public sealed partial class MainWindow : WindowEx
{
    private readonly ViewRegistrations viewRegistrations = new ViewRegistrations();

    public IDialogService DialogService { get; }

    public MainWindow(IMessenger messenger)
    {        
        this.InitializeComponent();
        Closed += MainWindow_Closed;
        DialogService = new DialogService(this);
        messenger.Subscribe<EnterFullscreenMessage>(msg => EnterFullscreen());
        messenger.Subscribe<ExitFullscreenMessage>(msg => ExitFullscreen());
        messenger.Subscribe<NavigateToPageMessage>(msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Subscribe<NavigateBackMessage>(msg => frame.GoBack());
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        Log.ArchiveLogFile();
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
