using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

internal static class DispatcherQueueUtil
{
    public static Task DispatchAsync(this DispatcherQueue dispatcherQueue, Action action)
    {
        if (dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }
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
        return tsc.Task;
    }

    public static Task DispatchAsync(this DispatcherQueue dispatcherQueue, Func<Task> function)
    {
        if (dispatcherQueue.HasThreadAccess)
        {
            return function();
        }
        var tsc = new TaskCompletionSource();
        dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await function();
                tsc.SetResult();
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }
        });
        return tsc.Task;
    }
}
