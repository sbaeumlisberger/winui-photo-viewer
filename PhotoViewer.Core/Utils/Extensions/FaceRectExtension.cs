using MetadataAPI.Data;
using Windows.Foundation;

namespace PhotoViewer.Core.Utils;

internal static class FaceRectExtension
{
    public static Rect ToRect(this FaceRect faceRect)
    {
        return new Rect(faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height);
    }
}
