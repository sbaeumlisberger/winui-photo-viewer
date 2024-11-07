using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Globalization;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using StringsApp = PhotoViewer.App.Resources.Strings;
using StringsCore = PhotoViewer.Core.Resources.Strings;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace PhotoViewer.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public ViewModelFactory ViewModelFactory { get; private set; } = null!;

    public MainWindow Window { get; private set; } = null!;

    private bool isUnhandeldExceptionDialogShown = false;

    private readonly ApplicationSettings applicationSettings;

    public App(ApplicationSettings applicationSettings)
    {
        this.applicationSettings = applicationSettings;
        InitializeComponent();
        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Log.Debug($"Handle OnLaunched");

        IMessenger messenger = new StrongReferenceMessenger();

        var backgroundTask = Task.Run(() =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var args = AppInstance.GetActivatedEventArgs();

            var mediaFilesLoaderService = new MediaFilesLoaderService(CachedImageLoaderService.Instance, new FileSystemService());

            var loadMediaFilesTask = LoadMediaFiles(args, applicationSettings, mediaFilesLoaderService);

            ViewModelFactory = new ViewModelFactory(applicationSettings, messenger, mediaFilesLoaderService);

            var mainWindowModel = ViewModelFactory.CreateMainWindowModel();

            Log.Debug($"OnLaunched background task completed in {stopwatch.ElapsedMilliseconds} ms");

            return (loadMediaFilesTask, mainWindowModel);
        });

        string language = ApplicationLanguages.Languages[0];
        StringsApp.Culture = new CultureInfo(language);
        StringsCore.Culture = new CultureInfo(language);

        Window = new MainWindow(messenger);
        ColorProfileProvider.Instance.InitializeAsync(Window);
        Window.Activate();

        var (loadMediaFilesTask, mainWindowModel) = await backgroundTask;

        Window.SetViewModel(mainWindowModel);

        messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel)));

        messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
    }

    private LoadMediaFilesTask LoadMediaFiles(IActivatedEventArgs activatedEventArgs, ApplicationSettings settings, IMediaFilesLoaderService mediaFilesLoaderService)
    {
        Log.Info($"Load files from {activatedEventArgs}");

        var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);

        if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
        {
            if (fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery is { } neighboringFilesQuery)
            {
                var startFile = (IStorageFile)fileActivatedEventArgsWithNeighboringFiles.Files[0];
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
        else if (Debugger.IsAttached)
        {
            return mediaFilesLoaderService.LoadFolder(KnownFolders.PicturesLibrary, config);
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

            var errorReportService = new ErrorReportService(Package.Current.Id.Version, new EventLogService());

            string report = await errorReportService.CreateErrorReportAsync();

            await Window.DispatcherQueue.DispatchAsync(async () =>
            {
                var dialog = new UnhandledExceptionDialog(args.Message, report);

                var dialogResult = await Window.ShowDialogAsync(dialog, dialog.GetResultAsync);

                if (dialog.IsSendErrorReportChecked)
                {
                    try
                    {
                        await errorReportService.SendErrorReportAsync(report);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to send error report: " + ex);
                    }
                }

                if (dialogResult == UnhandledExceptionDialogResult.Exit)
                {
                    Log.Info("User decided to exit application after unhandled exception ");
                    Exit();
                }
                else
                {
                    Log.Info("User decided to ignore unhandled exception");
                    isUnhandeldExceptionDialogShown = false;
                }
            });
        }
        catch (Exception e)
        {
            Log.Error("Failed to show unhandled exception dialog, exit application now", e);
            Exit();
        }
    }

}
