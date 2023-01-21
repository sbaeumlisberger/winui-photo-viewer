using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Services;
using PhotoViewerCore.Utils;
using PhotoViewerApp.Utils.Logging;
using System;
using WinRT.Interop;
using WinUIEx;
using PhotoViewerCore;

namespace PhotoViewerApp;

public sealed partial class MainWindow : WindowEx
{
    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    public MainWindow(IMessenger messenger)
    {
        this.InitializeComponent();
        Closed += MainWindow_Closed;
        ViewModelFactory.Instance.Initialize(new DialogService(this));
        messenger.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        messenger.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        messenger.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Register<NavigateBackMessage>(this, _ => frame.GoBack());
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
