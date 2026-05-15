namespace PhotoViewer.Test;

/// <summary>
/// Executes tasks synchronously on the calling thread.
/// </summary>
internal class FakeTaskScheduler : TaskScheduler
{
    protected override IEnumerable<Task> GetScheduledTasks() => Array.Empty<Task>();

    protected override void QueueTask(Task task)
    {
        TryExecuteTask(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return TryExecuteTask(task);
    }
}
