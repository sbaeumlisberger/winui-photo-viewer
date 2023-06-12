using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NSubstitute;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

    private readonly ApplicationSettings applicationSettings;

    private bool isUnhandeldExceptionDialogShown = false;

    public App()
    {
        var stopwatch = Stopwatch.StartNew();

        applicationSettings = LoadSettings();

        Log.Logger = new LoggerImpl(applicationSettings.IsDebugLogEnabled);

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

        var loadMediaFilesTask = LoadMediaFiles(applicationSettings);

        var messenger = StrongReferenceMessenger.Default;

        await CreateWindowAsync(messenger, applicationSettings);

        messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel)));

        stopwatch.Stop();
        Log.Info($"OnLaunched took {stopwatch.ElapsedMilliseconds}ms");

        messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));

        // TODO check event log for crashes
        // https://www.c-sharpcorner.com/UploadFile/d551d3/reading-and-querying-eventviewer-efficiently-with-C-Sharp/
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
            Log.Fatal($"An unhandled exception occurred: {args.Message}", exception);
        }
        else
        {
            Log.Fatal($"An unhandled exception occurred: {args.Message}");
        }

        args.Handled = true;

        if (Window?.Content?.XamlRoot is null)
        {
            Log.Info("Exit application after unhandled exception in startup");
            Exit();
        }
        else
        {
            ShowUnhandledExceptionDialog(args);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Log.Error("An unobserved task exception occurred", args.Exception);
    }

    private async void ShowUnhandledExceptionDialog(UnhandledExceptionEventArgs args)
    {
        if (isUnhandeldExceptionDialogShown)
        {
            return;
        }

        try
        {
            isUnhandeldExceptionDialogShown = true;

            var dialog = new UnhandledExceptionDialog(Window, args.Message);

            var dialogResult = await dialog.ShowAsync();

            if (dialog.IsSendErrorReportChecked)
            {
                Log.Info("Sending error report");
                await ErrorReportService.TrySendErrorReportAsync();
            }

            if (dialogResult == ContentDialogResult.Primary)
            {
                Log.Info("User decided to exit application after unhandled exception ");
                Exit();
            }
            else
            {
                Log.Info("User decided to ignore unhandled exception");
                isUnhandeldExceptionDialogShown = false;
            }
        }
        catch (Exception e)
        {
            Log.Error("Failed to show unhandled exception dialog, exit application now", e);
            Exit();
        }
    }

}
