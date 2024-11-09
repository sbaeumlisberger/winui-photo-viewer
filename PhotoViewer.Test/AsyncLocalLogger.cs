using Essentials.NET.Logging;
using System.Runtime.CompilerServices;

namespace PhotoViewer.Test;

internal class AsyncLocalLogger : ILogger
{
    public static AsyncLocalLogger Instance { get; } = new AsyncLocalLogger();

    public ILogger? Logger { get => logger.Value; set => logger.Value = value!; }

    private readonly AsyncLocal<ILogger> logger = new AsyncLocal<ILogger>();

    public IReadOnlyList<ILogAppender> Appenders => logger.Value?.Appenders ?? Array.Empty<ILogAppender>();

    private AsyncLocalLogger()
    {
    }

    public void Dispose()
    {
        logger.Value?.Dispose();
    }

    public void Debug(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        logger.Value?.Debug(message, exception, memberName, file, lineNumber);
    }

    public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
       logger.Value?.Info(message, memberName, file, lineNumber);
    }

    public void Warn(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        logger.Value?.Warn(message, exception, memberName, file, lineNumber);
    }

    public void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        logger.Value?.Error(message, exception, memberName, file, lineNumber);
    }

    public void Fatal(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        logger.Value?.Fatal(message, exception, memberName, file, lineNumber);
    }

    public void Write(LogLevel level, string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1)
    {
        logger.Value?.Write(level, message, exception, memberName, file, lineNumber);
    }
}
