using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

internal static class DispatcherQueueUtil
{

    public static async Task TryEnqueueIfRequiredAsync(this DispatcherQueue dispatcherQueue, Action action)
    {
        if (!dispatcherQueue.HasThreadAccess)
        {
            var tsc = new TaskCompletionSource();
            dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tsc.SetResult();
                }
                catch (Exception ex)
                {
                    tsc.SetException(ex);
                }
            });
            await tsc.Task;
        }
        else
        {
            action();
        }
    }

    public static async Task TryEnqueueIfRequiredAsync(this DispatcherQueue dispatcherQueue, Func<Task> action)
    {
        if (!dispatcherQueue.HasThreadAccess)
        {
            var tsc = new TaskCompletionSource();
            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await action();
                    tsc.SetResult();
                }
                catch (Exception ex)
                {
                    tsc.SetException(ex);
                }
            });
            await tsc.Task;
        }
        else
        {
            await action();
        }
    }
}
