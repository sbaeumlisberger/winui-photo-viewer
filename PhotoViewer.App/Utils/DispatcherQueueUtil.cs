using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

internal static class DispatcherQueueUtil
{
    public static Task DispatchAsync(this DispatcherQueue dispatcherQueue, Action action)
    {
        return DispatchAsync(dispatcherQueue, DispatcherQueuePriority.Normal, action);
    }

    public static Task DispatchAsync(this DispatcherQueue dispatcherQueue, DispatcherQueuePriority priority, Action action)
    {
        if (dispatcherQueue.HasThreadAccess)
        {
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Task.FromException(ex);
            }
        }
        var tsc = new TaskCompletionSource();
        dispatcherQueue.TryEnqueue(priority, () =>
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
        return DispatchAsync(dispatcherQueue, DispatcherQueuePriority.Normal, function);
    }

    public static Task DispatchAsync(this DispatcherQueue dispatcherQueue, DispatcherQueuePriority priority, Func<Task> function)
    {
        if (dispatcherQueue.HasThreadAccess)
        {
            return function();
        }
        var tsc = new TaskCompletionSource();
        dispatcherQueue.TryEnqueue(priority, async () =>
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
