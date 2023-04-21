using Microsoft.UI.Xaml.Media.Animation;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

public static class StoryboardUtil
{
    public static Task RunAsync(this Storyboard storyboard)
    {
        var tcs = new TaskCompletionSource();
        void Storyboard_Completed(object? sender, object args)
        {
            storyboard.Completed -= Storyboard_Completed;
            tcs.SetResult();
        };
        storyboard.Completed += Storyboard_Completed;
        storyboard.Begin();
        return tcs.Task;
    }
}

