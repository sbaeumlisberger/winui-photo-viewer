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
    private static int ElapsedMilliseconds => (int)(DateTimeOffset.Now - startTime).TotalMilliseconds;

    private static readonly DateTimeOffset startTime = DateTimeOffset.Now;

    private static bool logTimeUntilImageDrawn = false;

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            string startFilePath = args[0];
            string fileTypeExtension = Path.GetExtension(startFilePath).ToLower();
            if (BitmapFileInfo.CommonFileExtensions.Contains(fileTypeExtension))
            {
                CachedImageLoaderService.Instance.Preload(startFilePath);
                logTimeUntilImageDrawn = true;
            }
        }

        var loadSettingsAndSetupLoggingTask = Task.Run(() =>
        {
            var applicationSettings = new SettingsService().LoadSettings();
            SetupLogging(applicationSettings);
            Log.Debug($"Logging ready after {ElapsedMilliseconds} ms");
            return applicationSettings;
        });

        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start((_) =>
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            var synchronizationContext = new DispatcherQueueSynchronizationContext(dispatcherQueue);
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);

            var applicationSettings = loadSettingsAndSetupLoggingTask.GetAwaiter().GetResult();

            Log.Debug($"Calling App constructor after {ElapsedMilliseconds} ms");

            new App(applicationSettings);
        });

        return 0;
    }

    public static void NotifyImageDrawn()
    {
        if (logTimeUntilImageDrawn)
        {
            logTimeUntilImageDrawn = false;
            Log.Debug($"Image was drawn after {ElapsedMilliseconds} ms");
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
