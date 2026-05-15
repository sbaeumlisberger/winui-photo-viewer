using Essentials.NET;

namespace PhotoViewer.Test;

/// <summary>
/// Executes posted callbacks synchronously.
/// </summary>
internal class FakeSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state)
    {
        using (Apply())
        {
            callback(state);
        }
    }

    public IDisposable Apply()
    {
        var previousSynchronizationContext = Current;
        SetSynchronizationContext(this);
        return new DelegatingDisposable(() => SetSynchronizationContext(previousSynchronizationContext));
    }
}
