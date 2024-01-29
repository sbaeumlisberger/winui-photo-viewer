using Windows.Foundation;

namespace PhotoViewer.Core.Utils;

public static class RectExtension
{
    public static bool Intersects(this Rect rect, Rect otherRect)
    {
        return !(otherRect.Left > rect.Right || otherRect.Right < rect.Left || otherRect.Top > rect.Bottom || otherRect.Bottom < rect.Top);
    }

    public static double GetCenterX(this Rect rect)
    {
        return rect.Left + rect.Width / 2;
    }

    public static double GetCenterY(this Rect rect)
    {
        return rect.Top + rect.Height / 2;
    }
}
