using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Reflection;
using System.Threading.Tasks;
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

    public static void SetCursor(this FrameworkElement frameworkElement, InputCursor cursor)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance;
        typeof(UIElement).InvokeMember("ProtectedCursor", bindingFlags, null, frameworkElement, new object[] { cursor });
    }
}
