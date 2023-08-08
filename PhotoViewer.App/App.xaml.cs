using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using StringsApp = PhotoViewer.App.Resources.Strings;
using StringsCore = PhotoViewer.Core.Resources.Strings;

namespace PhotoViewer.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public ViewModelFactory ViewModelFactory { get; }

    public MainWindow Window { get; private set; } = null!;
    
    private readonly AppModel appModel;

    private bool isUnhandeldExceptionDialogShown = false;

    public App()
    {
        var stopwatch = Stopwatch.StartNew();

        var applicationSettings = new SettingsService().LoadSettings();

        Log.Logger = new LoggerImpl(applicationSettings.IsDebugLogEnabled);

        UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        string language = ApplicationLanguages.Languages[0];
        StringsApp.Culture = new CultureInfo(language);
        StringsCore.Culture = new CultureInfo(language);

        InitializeComponent();
       
        ViewModelFactory = new ViewModelFactory(applicationSettings);

        appModel = ViewModelFactory.CreateAppModel();

        stopwatch.Stop();
        Log.Info($"App constructor took {stopwatch.ElapsedMilliseconds}ms");
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs _)
    {
        var args = AppInstance.GetActivatedEventArgs();
        await appModel.OnLaunchedAsync(args, CreateWindowAsync);
        await TryReportCrashAsync();
    }

    private async Task CreateWindowAsync(MainWindowModel windowModel)
    {
        var stopwatch = Stopwatch.StartNew();
        Window = new MainWindow(windowModel);
        var initialiationTask = ColorProfileProvider.Instance.InitializeAsync(Window);
        Window.Activate();      
        await initialiationTask;
        stopwatch.Stop();
        Log.Info($"CreateWindowAsync took {stopwatch.ElapsedMilliseconds}ms");
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

            await Window.DispatcherQueue.TryEnqueueIfRequiredAsync(async () =>
            {
                var dialog = new UnhandledExceptionDialog(Window, args.Message);

                var dialogResult = await dialog.ShowAsync();

                if (dialog.IsSendErrorReportChecked)
                {
                    await TrySendErrorReportAsync();
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
            });
        }
        catch (Exception e)
        {
            Log.Error("Failed to show unhandled exception dialog, exit application now", e);
            Exit();
        }
    }

    private async Task TrySendErrorReportAsync() 
    {
        Log.Info("Sending error report");
        try
        {
            await new ErrorReportService().SendErrorReportAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to send crash report: " + ex);
        }
    }

    private async Task TryReportCrashAsync()
    {
        try
        {
            var errors = await Task.Run(() => new EventLogService().GetErrors());

            if (errors.Any())
            {
                var dialog = new CrashReportDialog(Window);

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await new ErrorReportService().SendCrashReportAsync(string.Join("\n\n", errors));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to check or report application crash", ex);
        }
    }


}
