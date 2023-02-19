using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace PhotoViewer.App.Utils.Logging;

public static class Log
{
    public static ILogger Logger { get; set; }

    [Conditional("DEBUG")]
    public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Logger.Debug(message, memberName, file, lineNumber);
    }

    public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Logger.Info(message, memberName, file, lineNumber);
    }

    public static void Warn(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Logger.Warn(message, exception, memberName, file, lineNumber);
    }

    public static void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Logger.Error(message, exception, memberName, file, lineNumber);
    }

    public static void Fatal(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Logger.Fatal(message, exception, memberName, file, lineNumber);
    }

    public static async Task<IStorageFile> GetLogFileAsync()
    {
        return await Logger.GetLogFileAsync().ConfigureAwait(false);
    }

    public static void ArchiveLogFile()
    {
        Logger.ArchiveLogFile();
    }
}
