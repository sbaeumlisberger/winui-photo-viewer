using Essentials.NET.Logging;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace PhotoViewer.Test;

internal class LoggerMock : ILogger
{
    public IReadOnlyList<ILogAppender> Appenders { get; } = [];

    private readonly ITestOutputHelper testOutputHelper;

    public void Dispose()
    {
        // not needed
    }

    public LoggerMock(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    public void Debug(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Write(LogLevel.DEBUG, message, exception, file, memberName, lineNumber);
    }

    public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Write(LogLevel.INFO, message, null, file, memberName, lineNumber);
    }

    public void Warn(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Write(LogLevel.WARN, message, exception, file, memberName, lineNumber);
    }

    public void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Write(LogLevel.ERROR, message, exception, file, memberName, lineNumber);
    }

    public void Fatal(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        Write(LogLevel.FATAL, message, exception, file, memberName, lineNumber);
    }

    public void Write(LogLevel level, string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var fileName = file.Substring(file.IndexOf(@"\PhotoViewer") + @"\PhotoViewer".Length);

        string line = ($"{timestamp} | {level} | {fileName}:{lineNumber} | {message}");

        try
        {
            testOutputHelper.WriteLine(line);
        }
        catch { }
    }
}
