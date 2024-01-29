using PhotoViewer.Core.Utils;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class ProgressTest
{
    // TODO

    [Fact]
    public async Task Report_ParallelAndOutOfOrder()
    {
        var progress = new Progress(null, new SynchronizationContextMock());

        var reportTasks = Enumerable.Range(1, 100)
            .Select(i => new Task(() => progress.Report(i / 100.0)))
            .ToList();

        reportTasks.ForEach(task => task.Start());

        await Task.WhenAll(reportTasks);

        await Task.Delay(100);

        Assert.Equal(1, progress.Value);
    }

}
