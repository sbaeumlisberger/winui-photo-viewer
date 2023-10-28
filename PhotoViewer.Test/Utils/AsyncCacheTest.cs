using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class AsyncCacheTest
{
    private record ValueObject(string Value);

    private const int CacheSize = 3;

    private readonly List<ValueObject> removedCallbackInvocations = new();

    private readonly AsyncCache<string, ValueObject> cache;

    public AsyncCacheTest() 
    {
        cache = new AsyncCache<string, ValueObject>(CacheSize, removedCallbackInvocations.Add);
    }

    [Fact]
    public async Task GetAsync_CreatesValue_WhenCachedTaskIsCanceled()
    {       
        string key = "some-key";
        try
        {
            var cancelledTask = Task.FromCanceled<ValueObject>(new CancellationToken(true));
            var task = cache.GetAsync(key, (_, _) => cancelledTask); 
        }
        catch (OperationCanceledException) { }

        Assert.Equal(1, cache.CurrentSize);

        var value = new ValueObject("some-value");
        var result = await cache.GetAsync(key, (_, _) => Task.FromResult(value));

        Assert.Equal(value, result);
        Assert.Equal(1, cache.CurrentSize);
    }

    [Fact]
    public void RemoveCallbackIsInvoked_WhenRemovedTaskCompletes() 
    {       
        var value1 = new ValueObject("test-01");
        var tsc1 = new TaskCompletionSource<ValueObject>();
        cache.GetAsync("key-01", (_, _) => tsc1.Task);
        cache.GetAsync("key-02", (_, _) => new TaskCompletionSource<ValueObject>().Task);
        cache.GetAsync("key-03", (_, _) => new TaskCompletionSource<ValueObject>().Task);

        cache.GetAsync("key-04", (_, _) => Task.FromResult(new ValueObject("test-04")));
               
        tsc1.SetResult(value1);

        Assert.Contains(value1, removedCallbackInvocations);
        Assert.Equal(CacheSize, cache.CurrentSize);
    }

}
