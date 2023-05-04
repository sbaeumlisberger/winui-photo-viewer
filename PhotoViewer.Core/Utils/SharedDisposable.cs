using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public interface ISharedDisposable : IDisposable
{
    void Request();
}

public abstract class SharedDisposableBase : ISharedDisposable
{
    private int useCount = 1;

    public void Request()
    {
        useCount++;
    }

    public void Dispose()
    {
        useCount--;

        if (useCount == 0)
        {
            Log.Debug("Dispose " + this);
            OnDispose();
        }
    }

    protected abstract void OnDispose();
}
