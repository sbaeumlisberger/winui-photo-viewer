using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public interface ICacheableDisposable : IDisposable
{
    void RequestUsage();
}

public abstract class CacheableDisposable : ICacheableDisposable
{
    private int usageCount = 1;

    public void RequestUsage()
    {
        usageCount++;
    }

    public void Dispose()
    {
        usageCount--;

        if (usageCount == 0)
        {
            Log.Debug("Dispose " + this);
            OnDispose();
        }
    }

    protected abstract void OnDispose();
}
