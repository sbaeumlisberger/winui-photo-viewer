using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace PhotoVieweApp.Utils;

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

}
