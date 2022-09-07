using Microsoft.UI.Xaml;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerApp.ViewModels;
using PhotoViewerApp.Views;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace PhotoViewerApp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public MainWindow Window { get; private set; } = null!;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        Log.Info($"Application started.");

        Log.Debug($"Local app folder: {ApplicationData.Current.LocalFolder.Path}");

        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user. Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Info($"Application launched.");

        var settingsService = new SettingsService();
        var settings = await settingsService.LoadSettingsAsync();
        ApplicationSettingsProvider.SetSettings(settings);
        var loadMediaItemsService = new MediaFilesLoaderService();
        var activatedEventArgs = AppInstance.GetActivatedEventArgs();
        var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName);
        var loadMediaItemsTask = loadMediaItemsService.LoadMediaFilesAsync(activatedEventArgs, config);

        var messenger = Messenger.GlobalInstance;

        Window = new MainWindow(messenger);
        Window.Activate();

        ColorProfileProvider.Initialize(Window);

        messenger.Publish(new NavigateToPageMessage(typeof(FlipViewPageModel)));

        var loadMediaItemsResult = await loadMediaItemsTask;
        messenger.Publish(new MediaItemsLoadedMessage(loadMediaItemsResult.MediaItems, loadMediaItemsResult.StartItem));
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.Exception is Exception exception)
        {
            Log.Fatal("An unhandled exception occurred", exception);
        }
        else
        {
            Log.Fatal($"An unhandled exception occurred: {args.Message}");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Log.Error("An unobserved task exception occurred", args.Exception);
    }

}
