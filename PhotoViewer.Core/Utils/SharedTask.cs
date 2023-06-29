using ABI.System;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

internal class SharedTask<T>
{
    private readonly CancellationTokenSource cts = new();

    private readonly List<CancellationToken> cancellationTokens = new();

    private Task<T>? task;

    public void Start(Func<CancellationToken, Task<T>> startTask)
    {
        task = startTask(cts.Token);
    }

    public Task<T> GetTask(CancellationToken cancellationToken = default)
    {
        if (task is null)
        {
            throw new InvalidOperationException();
        }

        var tsc = new TaskCompletionSource<T>();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationTokens.Add(cancellationToken);

            cancellationToken.Register(() =>
            {
                if (cancellationTokens.All(token => token.IsCancellationRequested))
                {
                    cts.Cancel();
                }
                tsc.TrySetCanceled();
            });
        }

        task.ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                tsc.TrySetException(task.Exception!);
            }
            else if (task.IsCompletedSuccessfully)
            {
                tsc.TrySetResult(task.Result);
            }
        });

        return tsc.Task;
    }

}