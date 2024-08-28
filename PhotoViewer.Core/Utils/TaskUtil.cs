using Essentials.NET.Logging;

namespace PhotoViewer.Core.Utils;

public static class TaskUtil
{

    public static async void LogOnException(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error("Task failed", ex);
        }
    }

}
