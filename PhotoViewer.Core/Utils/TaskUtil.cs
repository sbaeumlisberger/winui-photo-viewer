using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public static class TaskUtil
{

    public static async void LogOnException(this Task task) 
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error("Task failed", ex);
        }
    }

}
