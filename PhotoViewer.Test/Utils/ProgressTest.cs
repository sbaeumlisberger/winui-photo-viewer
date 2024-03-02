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
        using var _ = fakeSynchronizationContext.Apply();

        var timeProvider = new FakeTimeProvider();

        var progress = new Progress(null, timeProvider);

        SynchronizationContext? propertyChangedSynchronizationContext = null;

        progress.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Progress.Value))
            {
                propertyChangedSynchronizationContext = SynchronizationContext.Current;
            }
        };

        var reportTasks = Enumerable.Range(1, 100)
            .Select(i => new Task(() =>
            {
                using (new FakeSynchronizationContext().Apply())
                {
                    progress.Report(i / 100.0);
                }
            }))
            .ToList();

        reportTasks.ForEach(task => task.Start());

        await Task.WhenAll(reportTasks);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(fakeSynchronizationContext, propertyChangedSynchronizationContext);
        Assert.Equal(1, progress.Value);
    }

}
