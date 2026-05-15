using Microsoft.Extensions.Time.Testing;
using PhotoViewer.Core.Utils;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class ProgressTest
{
    // TODO

    private readonly FakeSynchronizationContext fakeSynchronizationContext = new FakeSynchronizationContext();

    [Fact]
    public async Task Report_ParallelAndOutOfOrder()
    {
        var timeProvider = new FakeTimeProvider();

        Progress progress;

        using (fakeSynchronizationContext.Apply())
        {
            progress = new Progress(null, timeProvider);
        }

        SynchronizationContext? propertyChangedSynchronizationContext = null;

        progress.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Progress.Value))
            {
                propertyChangedSynchronizationContext = SynchronizationContext.Current;
            }
        };

        var fakeTaskScheduler = new FakeTaskScheduler();

        var reportTasks = Enumerable.Range(1, 100)
            .Select(i => new Task(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);
                progress.Report(i / 100.0);
            }))
            .ToList();

        reportTasks.ForEach(task => task.Start(fakeTaskScheduler));

        await Task.WhenAll(reportTasks);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(fakeSynchronizationContext, propertyChangedSynchronizationContext);
        Assert.Equal(1, progress.Value);
    }
}
