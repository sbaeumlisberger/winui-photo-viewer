using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

internal static class DispatcherQueueUtil
{

    public static Task TryEnqueueIfRequiredAsync(this DispatcherQueue dispatcherQueue, Action action)
    {
        if (!dispatcherQueue.HasThreadAccess)
        {
            var tsc = new TaskCompletionSource();
            try
            {
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
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }
            return tsc.Task;
        }
        else
        {
            action();
            return Task.CompletedTask;
        }
    }
}
