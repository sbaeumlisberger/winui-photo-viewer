using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhotoViewer.App.Utils;

public static class FrameworkElementUtil
{

    public static void ShowAttachedFlyout(this FrameworkElement element, ContextRequestedEventArgs args)
    {
        var flyout = FlyoutBase.GetAttachedFlyout(element);

        if (flyout is MenuFlyout menuFlyout && args.TryGetPosition(element, out Point position))
        {
            menuFlyout.ShowAt(element, position);
        }
        else
        {
            flyout.ShowAt(element);
        }
    }

    public static async Task<bool> TryFocusAsync(this FrameworkElement frameworkElement)
    {
        for (int i = 0; i < 10; i++)
        {
            if (frameworkElement.Focus(FocusState.Programmatic))
            {
                return true;
            }
            await Task.Delay(10);
        }
        return false;
    }
}
