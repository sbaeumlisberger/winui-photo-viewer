using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Utils.Logging;

public interface ILogger
{
    void Debug(string value, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Info(string value, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Warn(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Error(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    void Fatal(string value, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = -1);

    Task<IStorageFile> GetLogFileAsync();

    Task ClearLogFileAsync();

    Task<IStorageFile> GetPreviousLogFileAsync();
}
