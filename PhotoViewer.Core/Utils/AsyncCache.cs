using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

internal class AsyncCache<TKey, TValue> where TKey : notnull
{
    public int CurrentSize => cache.CurrentSize;

    public int MaxSize => cache.MaxSize;

    private readonly Cache<TKey, SharedTask<TValue>> cache;

    private readonly Action<TValue>? removedCallback;

    public AsyncCache(int maxSize, Action<TValue>? removedCallback = null)
    {
        if (maxSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSize));
        }
        cache = new Cache<TKey, SharedTask<TValue>>(maxSize, InvokeRemovedCallback);
        this.removedCallback = removedCallback;
    }

    public void Remove(TKey key)
    {
        cache.Remove(key);
    }

    public Task<TValue> GetAsync(TKey key, Func<TKey, CancellationToken, Task<TValue>> createValueCallback, CancellationToken cancellationToken = default)
    {
        var sharedTask = cache.GetOrCreate(key, key =>
        {
            return SharedTask<TValue>.StartNew(cancellationToken => createValueCallback(key, cancellationToken));
        });

        var task = sharedTask.GetTask(cancellationToken);

        if (task.IsCanceled && !cancellationToken.IsCancellationRequested) 
        {
            cache.Remove(key);
            task = GetAsync(key, createValueCallback, cancellationToken);
        }

        return task;       
    }

    private void InvokeRemovedCallback(SharedTask<TValue> sharedTask) 
    {
        _ = sharedTask.InternalTask.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                removedCallback?.Invoke(task.Result);
            }
            sharedTask.Dispose();
        });
    }
}
