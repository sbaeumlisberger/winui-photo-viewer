using Essentials.NET;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewer.Core.Models;

public interface IBitmapImageModel : IShareableDisposable
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
