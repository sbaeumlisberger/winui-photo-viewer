using Microsoft.Extensions.Time.Testing;
using PhotoViewer.Core.Utils;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class ProgressTest
{
    // TODO

    [Fact]
    public async Task Report_ParallelAndOutOfOrder()
    {
        var timeProvider = new FakeTimeProvider();

        var progress = new Progress(null, new SynchronizationContextMock(), timeProvider);

        var reportTasks = Enumerable.Range(1, 100)
            .Select(i => new Task(() => progress.Report(i / 100.0)))
            .ToList();

        reportTasks.ForEach(task => task.Start());

        await Task.WhenAll(reportTasks);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Yield();

        Assert.Equal(1, progress.Value);
    }

}
