using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
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
using WinUIEx;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace PhotoViewer.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public MainWindow Window { get; private set; } = null!;

    public App()
    {
        var stopwatch = Stopwatch.StartNew();

        Log.Logger = new LoggerImpl();

        stopwatch.Stop();
        Log.Info($"LoggerImpl took {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Start();

        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        string language = ApplicationLanguages.Languages.First();
        Strings.Culture = new CultureInfo(language);
        PhotoViewer.Core.Resources.Strings.Culture = new CultureInfo(language);

        this.InitializeComponent();

        stopwatch.Stop();
        Log.Info($"App constructor took {stopwatch.ElapsedMilliseconds}ms");
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    { 
        //Log.Info("Application launched.");
        var stopwatch = Stopwatch.StartNew();       

        var settings = LoadSettings();

        var loadMediaFilesTask = LoadMediaFiles(settings);

        var messenger = StrongReferenceMessenger.Default;

        await CreateWindowAsync(messenger);

        messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel)));

        stopwatch.Stop();
        Log.Info($"OnLaunched took {stopwatch.ElapsedMilliseconds}ms");

        messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
    }

    private ApplicationSettings LoadSettings()
    {
        var settingsService = new SettingsService();
        var settings = settingsService.LoadSettings();
        ApplicationSettingsProvider.SetSettings(settings);
        return settings;
    }

    private LoadMediaFilesTask LoadMediaFiles(ApplicationSettings settings)
    {
        var loadMediaItemsService = new MediaFilesLoaderService();
        var activatedEventArgs = AppInstance.GetActivatedEventArgs();
        var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);
        return loadMediaItemsService.LoadMediaFilesFromActivateEventArgs(activatedEventArgs, config);
    }

    private Task CreateWindowAsync(IMessenger messenger)
    {
        var stopwatch = Stopwatch.StartNew();
        Window = new MainWindow(messenger);
        Window.Activate();
        var windowId = Win32Interop.GetWindowIdFromWindow(Window.GetWindowHandle());
        var task = ColorProfileProvider.Instance.InitializeAsync(windowId);
        stopwatch.Stop();
        Log.Info($"CreateWindowAsync took {stopwatch.ElapsedMilliseconds}ms");
        return task;
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
