using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class CacheableDisposableTest
{
    private class CacheableDisposableImpl : CacheableDisposable
    {
        public bool IsDisposed { get; private set; } = false;

        protected override void OnDispose()
        {
            IsDisposed = true;
        }
    }

    [Fact]
    public void OnDisposeIsCalled_WhenCreatedAndDisposed() 
    {
        var sharedDisposable = new CacheableDisposableImpl();
        sharedDisposable.Dispose();
        Assert.True(sharedDisposable.IsDisposed);
    }

    [Fact]
    public void OnDisposeIsCalled_WhenDisposedSameTimesAsRequested()
    {
        var sharedDisposable = new CacheableDisposableImpl();
        sharedDisposable.RequestUsage();

        sharedDisposable.Dispose();
        sharedDisposable.Dispose();

        Assert.True(sharedDisposable.IsDisposed);
    }

    [Fact]
    public void OnDisposeIsNotCalled_WhenDisposedLessTimesThanRequested()
    {
        var sharedDisposable = new CacheableDisposableImpl();
        sharedDisposable.RequestUsage();

        sharedDisposable.Dispose();

        Assert.False(sharedDisposable.IsDisposed);
    }

}
