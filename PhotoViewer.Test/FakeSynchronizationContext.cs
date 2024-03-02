using Essentials.NET;

namespace PhotoViewer.Test
{
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
}
