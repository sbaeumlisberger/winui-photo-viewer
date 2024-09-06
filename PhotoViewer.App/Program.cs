using Essentials.NET.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.App;

public static class Program
{
    public static Task ImageDrawnTask { get; private set; } = Task.CompletedTask;

    private static readonly Stopwatch stopwatch = Stopwatch.StartNew();

    private static bool isImageDrawed = false;

    private static TaskCompletionSource? imageDrawnTaskCompletionSource;

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            string startFilePath = args[0];
            CachedImageLoaderService.Instance.Preload(startFilePath);
            imageDrawnTaskCompletionSource = new TaskCompletionSource();
            ImageDrawnTask = imageDrawnTaskCompletionSource.Task;
        }

        var loadSettingsAndSetupLoggingTask = Task.Run(() =>
        {
            var applicationSettings = new SettingsService().LoadSettings();
            SetupLogging(applicationSettings);
            return applicationSettings;
        });

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start((_) =>
        {
            File.AppendAllText(Path.Combine(AppData.PublicFolder, "main.txt"), stopwatch.ElapsedMilliseconds + "\n");
            var synchronizationContext = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            new App(loadSettingsAndSetupLoggingTask);
        });
        return 0;
    }

    public static void NotifyStartupCompleted()
    {
        //stopwatch.Stop();
        File.AppendAllText(Path.Combine(AppData.PublicFolder, "startup.txt"), stopwatch.ElapsedMilliseconds + "\n");
    }

    public static void NotifyImageDrawn()
    {
        imageDrawnTaskCompletionSource?.TrySetResult();

        if (!isImageDrawed)
        {
            isImageDrawed = true;
            stopwatch.Stop();
            Log.Debug($"Image was drawn after {stopwatch.ElapsedMilliseconds} ms");
            File.AppendAllText(Path.Combine(AppData.PublicFolder, "drawed.txt"), stopwatch.ElapsedMilliseconds + "\n");
        }
    }

    private static void SetupLogging(ApplicationSettings applicationSettings, [CallerFilePath] string callerFilePath = "")
    {
        int filePathOffset = callerFilePath.IndexOf(@"App\Program.cs", StringComparison.Ordinal);
        LogLevel logLevel = applicationSettings.IsDebugLogEnabled || Debugger.IsAttached ? LogLevel.DEBUG : LogLevel.INFO;
        var fileAppender = new FileAppender(Path.Combine(AppData.PublicFolder, "logs"), logLevel);
        var logFormat = new DefaultLogFormat(filePathOffset: filePathOffset);
        Log.Configure(new Logger([new DebugAppender(), fileAppender], logFormat));
    }
}
