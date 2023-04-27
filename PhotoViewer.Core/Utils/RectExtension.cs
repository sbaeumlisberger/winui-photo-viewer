using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace PhotoViewer.Core.Utils;

public static class RectExtension
{
    public static bool Intersects(this Rect rect, Rect otherRect)
    {
        return !(otherRect.Left > rect.Right || otherRect.Right < rect.Left || otherRect.Top > rect.Bottom || otherRect.Bottom < rect.Top);
    }
}
