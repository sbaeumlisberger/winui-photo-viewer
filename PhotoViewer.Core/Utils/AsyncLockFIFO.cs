namespace PhotoViewer.Core.Utils;

public class AsyncLockFIFO
{
    private Task waitTask = Task.CompletedTask;

    public Task<IDisposable> AcquireAsync()
    {
        lock (this)
        {
            var lockTSC = new TaskCompletionSource();
            var acquireTask = waitTask.IsCompleted
               ? Task.FromResult<IDisposable>(new DisposableAction(() => lockTSC.SetResult()))
               : waitTask.ContinueWith<IDisposable>(_ => new DisposableAction(() => lockTSC.SetResult()));
            waitTask = lockTSC.Task;
            return acquireTask;
        }
    }

}
