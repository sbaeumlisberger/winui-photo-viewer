using PhotoViewer.App.Utils;
using System.Collections.Concurrent;

namespace PhotoViewer.Core.Utils;

internal class SharedTask<T> : IDisposable
{
    private readonly CancellationTokenSource cts = new();

    private readonly ConcurrentBag<CancellationToken> cancellationTokens = new();

    private readonly ConcurrentBag<CancellationTokenRegistration> cancellationTokenRegistrations = new();

    private readonly Task<T> task;

    private bool isDisposed = false;

    internal Task<T> InternalTask => task;

    public static SharedTask<T> StartNew(Func<CancellationToken, Task<T>> function)
    {
        return new SharedTask<T>(function);
    }

    private SharedTask(Func<CancellationToken, Task<T>> function)
    {
        task = function(cts.Token);
    }

    public Task<T> GetTask(CancellationToken cancellationToken = default)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(typeof(SharedTask<T>).FullName);
        }

        if (task.IsCompleted)
        {
            return task;
        }

        var tsc = new TaskCompletionSource<T>();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationTokens.Add(cancellationToken);

            cancellationTokenRegistrations.Add(cancellationToken.Register(() =>
            {
                if (cancellationTokens.All(token => token.IsCancellationRequested))
                {
                    cts.Cancel();
                }
                tsc.TrySetCanceled();
            }));
        }

        if (cts.Token.IsCancellationRequested)
        {
            tsc.TrySetCanceled();
        }
        else
        {
            task.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    tsc.TrySetResult(task.Result);
                }
                else if (task.IsFaulted)
                {
                    tsc.TrySetException(task.Exception!);
                }
                else if (task.IsCanceled)
                {
                    tsc.TrySetCanceled();
                }
            });
        }

        return tsc.Task;
    }

    public void Dispose()
    {
        isDisposed = true;
        cancellationTokenRegistrations.ForEach(registration => registration.Dispose());
        cancellationTokenRegistrations.Clear();
        cts.Dispose();
    }
}