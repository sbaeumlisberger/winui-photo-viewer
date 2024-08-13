using Essentials.NET.Logging;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace PhotoViewer.Test;

internal class LoggerMock : ILogger
{
    private readonly ITestOutputHelper testOutputHelper;

    public LoggerMock(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    public void Debug(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Log("DEBUG", message, exception, file, lineNumber);
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

    public void Dispose()
    {
        // not needed
    }

    private void Log(string level, string? message, Exception? exception, string file, int lineNumber)
    {
        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var fileName = file.Substring(file.IndexOf(@"\PhotoViewer") + @"\PhotoViewer".Length);

        string line = ($"{timestamp} | {level} | {fileName}:{lineNumber} | {message ?? exception?.Message ?? ""}");

        try
        {
            testOutputHelper.WriteLine(line);
        }
        catch { }
    }
}
