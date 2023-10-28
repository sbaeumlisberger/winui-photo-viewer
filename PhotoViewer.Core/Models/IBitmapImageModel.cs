using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Utils;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewer.App.Models;

public interface IBitmapImageModel : ICacheableDisposable
{
    string ID { get; }

    CanvasDevice Device { get; }

    ICanvasImage CanvasImage { get; }

    IReadOnlyList<IBitmapFrameModel> Frames { get; }

    Size SizeInDIPs { get; }

    BitmapSize SizeInPixels { get; }

    ColorSpaceInfo ColorSpace { get; }
}

public interface ICanvasBitmapImageModel : IBitmapImageModel
{
    CanvasBitmap CanvasBitmap { get; }
}

public interface ICanvasVirtualBitmapImageModel : IBitmapImageModel
{
    CanvasVirtualBitmap CanvasVirtualBitmap { get; }
}
