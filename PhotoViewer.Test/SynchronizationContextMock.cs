using Essentials.NET;

namespace PhotoViewer.Test
{
    internal class SynchronizationContextMock : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
            using var _ = Apply();
            callback(state);
        }

        public IDisposable Apply()
        {
            var oldSyncContext = Current;
            SetSynchronizationContext(this);
            return new DelegatingDisposable(() => SetSynchronizationContext(oldSyncContext));
        }
    }
}
