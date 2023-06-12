namespace PhotoViewer.Core.Utils;

public class AsyncLockFIFO
{
    public class AcquiredLock : IDisposable
    {
        private readonly Action action;

        public AcquiredLock(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action();
        }
    }

    private Task waitTask = Task.CompletedTask;

    public Task<AcquiredLock> AcquireAsync()
    {
        lock (this)
        {
            var lockTSC = new TaskCompletionSource();
            var acquireTask = waitTask.ContinueWith(_ => new AcquiredLock(() => lockTSC.SetResult()));
            waitTask = lockTSC.Task;
            return acquireTask;
        }
    }

}
