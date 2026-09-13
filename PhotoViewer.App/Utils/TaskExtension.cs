using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

internal static class TaskExtension
{
    public static ConfiguredTaskAwaitable SuppressThrowing(this Task task)
    {
        return task.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
    }
}
