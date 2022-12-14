using System.Diagnostics.CodeAnalysis;

namespace PhotoViewerApp.Models;

internal class AsyncCache<T>
{
    private Task<T>? task;

    public Task<T> GetOrCreateValueAsync(Func<Task<T>> createValueCallback)
    {
        if (task != null)
        {
            return task;
        }
        else
        {
            task = createValueCallback.Invoke();
            return task;
        }
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        if (task != null && task.IsCompleted)
        {
            value = task.Result!;
            return true;
        }
        value = default;
        return false;
    }
}