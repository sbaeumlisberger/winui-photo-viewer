using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class SharedDisposableBaseTest
{
    private class SharedDisposable : SharedDisposableBase
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
        var sharedDisposable = new SharedDisposable();
        sharedDisposable.Dispose();
        Assert.True(sharedDisposable.IsDisposed);
    }

    [Fact]
    public void OnDisposeIsCalled_WhenDisposedSameTimesAsRequested()
    {
        var sharedDisposable = new SharedDisposable();
        sharedDisposable.Request();

        sharedDisposable.Dispose();
        sharedDisposable.Dispose();

        Assert.True(sharedDisposable.IsDisposed);
    }

    [Fact]
    public void OnDisposeIsNotCalled_WhenDisposedLessTimesThanRequested()
    {
        var sharedDisposable = new SharedDisposable();
        sharedDisposable.Request();

        sharedDisposable.Dispose();

        Assert.False(sharedDisposable.IsDisposed);
    }

}
