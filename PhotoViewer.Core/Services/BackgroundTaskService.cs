using Essentials.NET;

namespace PhotoViewer.Core.Services;

public interface IBackgroundTaskService
{
    IObservableReadOnlyList<Task> BackgroundTasks { get; }

    void RegisterBackgroundTask(Task backgroundTask);
}

internal class BackgroundTaskService : IBackgroundTaskService
{
    public IObservableReadOnlyList<Task> BackgroundTasks => backgroundTasks;

    private readonly ObservableList<Task> backgroundTasks = new ObservableList<Task>();

    public void RegisterBackgroundTask(Task backgroundTask)
    {
        lock (backgroundTasks)
        {
            backgroundTasks.Add(backgroundTask);
        }

        _ = backgroundTask.ContinueWith(_ =>
        {
            lock (backgroundTasks)
            {
                backgroundTasks.Remove(backgroundTask);
            }
        });
    }

}
