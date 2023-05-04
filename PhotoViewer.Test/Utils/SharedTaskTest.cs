using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class SharedTaskTest
{
    [Fact]
    public async Task ReturnsValueToAllCallers()
    {
        string value = "Test Value";
        var tsc = new TaskCompletionSource();
        var sharedTask = new SharedTask<string>();
        sharedTask.Start(async cancellationToken =>
        {
            await tsc.Task;
            return value;
        });
        var task1 = sharedTask.GetTask();
        var task2 = sharedTask.GetTask();

        tsc.SetResult();

        Assert.Equal(value, await task1);
        Assert.Equal(value, await task2);
    }

    [Fact]
    public void CancelsTask_WhenAllCallersCanceld() 
    {
        var tsc = new TaskCompletionSource<string>();
        CancellationToken? sharedTaskCancellationToken = null;
        var sharedTask = new SharedTask<string>();
        sharedTask.Start(cancellationToken => 
        {
            sharedTaskCancellationToken = cancellationToken;
            return tsc.Task;
        });
        var cts1 = new CancellationTokenSource();
        var task1 = sharedTask.GetTask(cts1.Token);
        var cts2 = new CancellationTokenSource();
        var task2 = sharedTask.GetTask(cts2.Token);

        cts1.Cancel();
        Assert.False(sharedTaskCancellationToken!.Value.IsCancellationRequested);
        Assert.True(task1.IsCanceled);
        Assert.False(task2.IsCanceled);

        cts2.Cancel();
        Assert.True(sharedTaskCancellationToken.Value.IsCancellationRequested);
        Assert.True(task1.IsCanceled);
        Assert.True(task2.IsCanceled);
    }

    [Fact]
    public async Task ReportsExceptionToAllCallers()
    {
        var tsc = new TaskCompletionSource();
        var sharedTask = new SharedTask<string>();
        sharedTask.Start(async cancellationToken =>
        {
            await tsc.Task;
            throw new Exception("Test Exception");
        });
        var task1 = sharedTask.GetTask();
        var task2 = sharedTask.GetTask();

        tsc.SetResult();

        await Assert.ThrowsAnyAsync<Exception>(() => task1);
        await Assert.ThrowsAnyAsync<Exception>(() => task2);
    }
}
