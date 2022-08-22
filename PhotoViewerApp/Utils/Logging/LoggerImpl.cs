using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Utils.Logging;

internal class LoggerImpl : ILogger
{
    private static readonly string LogsFolderName = "logs";

    private static readonly string LogFileName = "log.txt";

    private static readonly string RolloverLogFileName = "previous-log.txt";

    private static readonly string LogFolderPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, LogsFolderName);

    private static readonly string LogFilePath = Path.Combine(LogFolderPath, LogFileName);

    private static readonly string RolloverLogFilePath = Path.Combine(LogFolderPath, RolloverLogFileName);

    private readonly StreamWriter logFileWriter;

    public LoggerImpl()
    {
        Directory.CreateDirectory(LogFolderPath);

        if (File.Exists(LogFilePath))
        {
            File.Delete(RolloverLogFilePath);
            File.Move(LogFilePath, RolloverLogFilePath);
        }

        var stream = new FileStream(LogFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        stream.Seek(0, SeekOrigin.End);
        logFileWriter = new StreamWriter(stream) { AutoFlush = true };
    }

    public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("DEBUG", message, null, file, lineNumber);
    }

    public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("INFO ", message, null, file, lineNumber);
    }

    public void Warn(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("WARN ", message, exception, file, lineNumber);
    }

    public void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("ERROR", message, exception, file, lineNumber);
    }

    public void Fatal(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("FATAL", message, exception, file, lineNumber);
    }

    private void Log(string prefix, string? message, Exception? exception, string file, int lineNumber)
    {
        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var fileName = file.Substring(file.IndexOf(@"\PhotoViewerApp\") + @"\PhotoViewerApp\".Length);

        AppendToLog($"{timestamp} | {prefix} | {fileName}:{lineNumber} | {message ?? exception?.Message ?? ""} \n");

        if (exception != null)
        {
            AppendToLog(ExceptionFormatter.Format(exception));
        }
    }

    private void AppendToLog(string logMessage)
    {
        PrintToDebugConsole(logMessage);
        AppendToLogFile(logMessage);
    }

    [Conditional("DEBUG")]
    private void PrintToDebugConsole(string logMessage)
    {
        System.Diagnostics.Debug.Write(logMessage);
    }

    private void AppendToLogFile(string logMessage)
    {
        try
        {
            logFileWriter.Write(logMessage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Could not write to log file. " + ex.ToString());
        }
    }

    public async Task<IStorageFile> GetLogFileAsync()
    {
        return await StorageFile.GetFileFromPathAsync(LogFilePath).AsTask().ConfigureAwait(false);
    }

    public async Task ClearLogFileAsync()
    {
        logFileWriter.BaseStream.Position = 0;
        logFileWriter.BaseStream.SetLength(0);
        await logFileWriter.FlushAsync().ConfigureAwait(false);
    }

    public async Task<IStorageFile> GetPreviousLogFileAsync()
    {
        return await StorageFile.GetFileFromPathAsync(RolloverLogFilePath).AsTask().ConfigureAwait(false);
    }
}
