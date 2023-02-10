using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Services.Maps;
using Windows.Storage;
using WinUIEx;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace PhotoViewer.App;

public partial class App : Application
{
    private const string MapServiceToken = "vQDj7umE60UMzHG2XfCm~ehfqvBJAFQn6pphOPVbDsQ~ArtM_t2j4AyKdgLIa5iXeftg8bEG4YRYCwhUN-SMXhIK73mnPtCYU4nOF2VtqGiF";

    public static new App Current => (App)Application.Current;

    public MainWindow Window { get; private set; } = null!;

    public App()
    {
        Debug.WriteLine($"Local app data folder: {ApplicationData.Current.LocalFolder.Path}");

        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        MapService.ServiceToken = MapServiceToken;

        string language = ApplicationLanguages.Languages.First();
        Strings.Culture = new CultureInfo(language);
        PhotoViewer.Core.Resources.Strings.Culture = new CultureInfo(language);

        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Info("Application launched.");

        var settingsService = new SettingsService();
        var settings = await settingsService.LoadSettingsAsync();
        ApplicationSettingsProvider.SetSettings(settings);

        var loadMediaItemsService = new MediaFilesLoaderService();
        var activatedEventArgs = AppInstance.GetActivatedEventArgs();
        var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);
        var loadMediaItemsTask = loadMediaItemsService.LoadMediaFilesAsync(activatedEventArgs, config);

        var messenger = StrongReferenceMessenger.Default;

        Window = new MainWindow(messenger);     
        Window.Activate();

        await ColorProfileProvider.Instance.InitializeAsync(Window.GetAppWindow().Id);

        messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel)));

        var loadMediaItemsResult = await loadMediaItemsTask;
        messenger.Send(new MediaFilesLoadedMessage(loadMediaItemsResult.MediaItems, loadMediaItemsResult.StartItem));
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
