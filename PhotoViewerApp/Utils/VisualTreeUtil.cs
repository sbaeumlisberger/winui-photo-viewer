using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.Utils;
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

}
