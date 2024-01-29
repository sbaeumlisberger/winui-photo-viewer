using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace PhotoViewer.App.Utils;
internal static class VisualTreeUtil
{

    public static FrameworkElement? FindChild(this FrameworkElement element, string name)
    {
        int count = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(element, i) is FrameworkElement child)
            {
                if (child.Name.Equals(name))
                {
                    return child;
                }
                if (FindChild(child, name) is FrameworkElement result)
                {
                    return result;
                }
            }
        }
        return null;
    }

    public static T? FindParent<T>(this FrameworkElement element) where T : FrameworkElement
    {
        var parent = VisualTreeHelper.GetParent(element);

        while (parent is not null && parent is not T)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return (T?)parent;
    }
}
