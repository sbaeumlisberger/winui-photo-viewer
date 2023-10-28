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
    public async Task GetTask_ReturnsValue_OnCompletion()
    {
        string value = "Test Value";
        var tsc = new TaskCompletionSource();
        var sharedTask = SharedTask<string>.StartNew(async cancellationToken =>
        {
            await tsc.Task;
            return value;
        });

        var task1 = sharedTask.GetTask();
        var task2 = sharedTask.GetTask();

        Assert.False(task1.IsCompleted);
        Assert.False(task2.IsCompleted);

        tsc.SetResult();

        Assert.Equal(value, await task1);
        Assert.Equal(value, await task2);
    }

    [Fact]
    public void StartNew_IsCancelled_WhenAllCallersRequestedCancellation() 
    {
        var tsc = new TaskCompletionSource<string>();
        CancellationToken? sharedTaskCancellationToken = null;
        var sharedTask = SharedTask<string>.StartNew(cancellationToken => 
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
    public async Task GetTask_ThrowsException_WhenExceptionIsThrown()
    {
        var tsc = new TaskCompletionSource<string>();
        var sharedTask = SharedTask<string>.StartNew(cancellationToken => tsc.Task);

        var task1 = sharedTask.GetTask();
        var task2 = sharedTask.GetTask();

        tsc.SetException(new Exception("Test Exception"));

        await Assert.ThrowsAnyAsync<Exception>(() => task1);
        await Assert.ThrowsAnyAsync<Exception>(() => task2);
    }

    [Fact]
    public void GetTask_ReturnsImmediately_WhenAlreadyCompleted() 
    {
        var sharedTask = SharedTask<string>.StartNew(cancellationToken => Task.FromResult("test"));

        var task = sharedTask.GetTask();

        Assert.True(task.IsCompleted);
    }

}
