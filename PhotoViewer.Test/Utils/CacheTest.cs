using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class CacheTest
{
    private const int CacheSize = 5;

    private record ValueObject(string Value);

    private Cache<string, ValueObject> cache = new Cache<string, ValueObject>(CacheSize);

    [Fact]
    public void GetOrCreate_ReturnsCachedValue() 
    {
        var value = new ValueObject("value 1");
        cache.GetOrCreate("key 1", key => value);
       
        var result = cache.GetOrCreate("key 1", key => new ValueObject("value 1"));

        Assert.Same(value, result);
    }


    [Fact]
    public async void GetOrCreate_HandlesParallelAccess()
    {
        int createValueCallbackInovcationCount = 0;
        ValueObject createValueCallback(string key) 
        { 
            createValueCallbackInovcationCount++;
            return new ValueObject("value 1");
        }

        var tasks = new Task[1000];
        for (int i = 0; i < tasks.Length; i++) 
        {
            tasks[i] = new Task(() => cache.GetOrCreate("key 1", createValueCallback));
        }
        tasks.ForEach(task => task.Start());
        await Task.WhenAll(tasks);
                 
        Assert.Equal(1, createValueCallbackInovcationCount);
    }


    [Fact]
    public void GetOrCreate_RemovedOldestEntryWhenCacheSizeExceeded()
    {
        var first = new ValueObject("value 1");
        cache.GetOrCreate("key 1", key => first);

        for (int i = 2; i <= CacheSize + 1; i++)
        {
            cache.GetOrCreate("key " + i, key => new ValueObject("value " + i));
        }

        Assert.DoesNotContain(first, cache.Values);
    }

    [Fact]
    public void GetOrCreate_UpdatesPositionInCache()
    {
        var first = new ValueObject("value 1");
        cache.GetOrCreate("key 1", key => first);
        for (int i = 2; i <= CacheSize; i++)
        {
            cache.GetOrCreate("key " + i, key => new ValueObject("value " + i));
        }

        cache.GetOrCreate("key 1", key => new ValueObject("value 1"));

        Assert.Same(first, cache.Values.Last());
    }

    [Fact]
    public void RemoveReturnsTrueWhenEntryFound()
    {
        cache.GetOrCreate("key 1", key => new ValueObject("value 1"));

        bool result = cache.Remove("key 1");
        
        Assert.True(result);
        Assert.Empty(cache.Values);
    }


    [Fact]
    public void RemoveReturnsFalseWhenEntryNotFound()
    {
         bool result = cache.Remove("key 1");

        Assert.False(result);
    }

}
