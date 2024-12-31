using MetadataAPI.Data;
using Windows.Foundation;

namespace PhotoViewer.Core.Utils;

internal class PeopleTagUtil
{
    public static FaceRect ToFaceRect(Rect rect, PhotoOrientation orientation)
    {
        var rotated = ReverseRotateRect(rect, orientation);
        return new FaceRect(rotated.X, rotated.Y, rotated.Width, rotated.Height);
    }

    private static Rect ReverseRotateRect(Rect rect, PhotoOrientation orientation)
    {
        return orientation switch
        {
            PhotoOrientation.Normal or PhotoOrientation.Unspecified => rect,
            PhotoOrientation.Rotate90 => new Rect(1 - rect.Bottom, rect.X, rect.Height, rect.Width),
            PhotoOrientation.Rotate180 => new Rect(1 - rect.Right, 1 - rect.Bottom, rect.Width, rect.Height),
            PhotoOrientation.Rotate270 => new Rect(rect.Y, 1 - rect.Right, rect.Height, rect.Width),
            _ => throw new NotSupportedException("Unsupported orientation: " + orientation),
        };
    }
}
