using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace PhotoViewerApp.Utils.Logging;

internal class LoggerImpl : ILogger
{
    private static readonly string LogFolderPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs");

    private static readonly string LogFilePath = Path.Combine(LogFolderPath, "log-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt");

    private readonly StreamWriter logFileWriter;

    public LoggerImpl()
    {
        Directory.CreateDirectory(LogFolderPath);

        var stream = new FileStream(LogFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        stream.Seek(0, SeekOrigin.End);
        logFileWriter = new StreamWriter(stream) { AutoFlush = true };

        System.Diagnostics.Debug.WriteLine($"Logging to: {LogFilePath}");

        Task.Run(CleanupOldLogFiles);
    }

    public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("DEBUG", message, null, file, lineNumber);
    }

    public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("INFO", message, null, file, lineNumber);
    }

    public void Warn(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("WARN", message, exception, file, lineNumber);
    }

    public void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("ERROR", message, exception, file, lineNumber);
    }

    public void Fatal(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("FATAL", message, exception, file, lineNumber);
    }

    public async Task<IStorageFile> GetLogFileAsync()
    {
        return await StorageFile.GetFileFromPathAsync(LogFilePath).AsTask().ConfigureAwait(false);
    }

    public void ArchiveLogFile()
    {
        logFileWriter.Close();
        File.Move(LogFilePath, LogFilePath + ".bak");
    }

    private void Log(string level, string? message, Exception? exception, string file, int lineNumber)
    {
        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var fileName = file.Substring(file.IndexOf(@"\PhotoViewer") + @"\PhotoViewer".Length);

        string line = ($"{timestamp} | {level} | {fileName}:{lineNumber} | {message ?? exception?.Message ?? ""} \n");

        if (exception != null)
        {
            AppendToLog(line + ExceptionFormatter.Format(exception));
        }
        else
        {
            AppendToLog(line);
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

    private void CleanupOldLogFiles()
    {
        try
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                Directory.EnumerateFiles(LogFolderPath)
                    .Where(filePath => filePath != LogFilePath)
                    .ForEach(filePath => TryDeleteFile(filePath));
                return;
            }
#endif
            var oneWeekAgo = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));
            Directory.EnumerateFiles(LogFolderPath)
                .Where(filePath => File.GetLastWriteTimeUtc(filePath) < oneWeekAgo)
                .ForEach(filePath => TryDeleteFile(filePath));
        }
        catch (Exception ex)
        {
            Warn("Failed to cleanup old log files", ex);
        }
    }

    private void TryDeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Warn($"Failed to delete \"{Path.GetFileName(filePath)}\"", ex);
        }
    }

}
