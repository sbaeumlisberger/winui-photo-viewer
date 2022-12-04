using Microsoft.UI.Xaml;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Services.Maps;
using Windows.Storage;
using WinUIEx;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace PhotoViewerApp;

public partial class App : Application
{
    public const string MapServiceToken = "vQDj7umE60UMzHG2XfCm~ehfqvBJAFQn6pphOPVbDsQ~ArtM_t2j4AyKdgLIa5iXeftg8bEG4YRYCwhUN-SMXhIK73mnPtCYU4nOF2VtqGiF";

    public static new App Current => (App)Application.Current;

    public MainWindow Window { get; private set; } = null!;

    public App()
    {
        Debug.WriteLine($"Local app data folder: {ApplicationData.Current.LocalFolder.Path}");

        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        this.InitializeComponent();

        MapService.ServiceToken = MapServiceToken;

        Log.Info($"Application instance created.");
    }


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

        await ColorProfileProvider.Instance.InitializeAsync(Window.GetAppWindow().Id);

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
