using System.Runtime.CompilerServices;
using Windows.Storage;

namespace PhotoViewer.App.Utils.Logging;

public interface ILogger
{
    void Debug(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Info(string value, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Warn(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Error(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Fatal(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    Task<IStorageFile> GetLogFileAsync();

    void ArchiveLogFile();
}
