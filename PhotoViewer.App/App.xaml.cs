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
using Windows.ApplicationModel.Activation;
using Windows.Globalization;
using Windows.Storage;
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
        Log.Info("Application launched");
        var stopwatch = Stopwatch.StartNew();

        var settings = LoadSettings();

        var loadMediaFilesTask = LoadMediaFiles(settings);

        var messenger = StrongReferenceMessenger.Default;

        await CreateWindowAsync(messenger, settings);

        messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel)));

        stopwatch.Stop();
        Log.Info($"OnLaunched took {stopwatch.ElapsedMilliseconds}ms");

        messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
    }

    private ApplicationSettings LoadSettings()
    {
        ISettingsService settingsService = new SettingsService();
        var settings = settingsService.LoadSettings();
        return settings;
    }

    private LoadMediaFilesTask LoadMediaFiles(ApplicationSettings settings)
    {
        var activatedEventArgs = AppInstance.GetActivatedEventArgs();

        Log.Info($"Enter LoadMediaFiles ({activatedEventArgs})");

        IMediaFilesLoaderService mediaFilesLoaderService = new MediaFilesLoaderService();
        var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);

        if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
        {
            if (fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery is { } neighboringFilesQuery)
            {
                var startFile = (IStorageFile)fileActivatedEventArgsWithNeighboringFiles.Files.First();
                return mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);
            }
            else
            {
                var activatedFiles = fileActivatedEventArgsWithNeighboringFiles.Files.Cast<IStorageFile>().ToList();
                return mediaFilesLoaderService.LoadFileList(activatedFiles, config);
            }
        }
        else if (activatedEventArgs is IFileActivatedEventArgs fileActivatedEventArgs)
        {
            var activatedFiles = fileActivatedEventArgs.Files.Cast<IStorageFile>().ToList();
            return mediaFilesLoaderService.LoadFileList(activatedFiles, config);
        }
        else if (Environment.GetCommandLineArgs().Length > 1)
        {
            var arguments = Environment.GetCommandLineArgs().Skip(1).ToList();
            return mediaFilesLoaderService.LoadFromArguments(arguments, config);
        }
        else
        {
#if DEBUG            
            return mediaFilesLoaderService.LoadFolder(KnownFolders.PicturesLibrary, config);
#else
            return LoadMediaFilesTask.Empty;
#endif
        }
    }

    private Task CreateWindowAsync(IMessenger messenger, ApplicationSettings settings)
    {
        var stopwatch = Stopwatch.StartNew();
        Window = new MainWindow(messenger, settings);
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
        Exit(); // TODO show dialog
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Log.Error("An unobserved task exception occurred", args.Exception);
    }

}
