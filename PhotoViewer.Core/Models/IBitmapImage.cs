using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewer.App.Models;

public interface IBitmapImage : IDisposable
{
    string ID { get; }

    CanvasDevice Device { get; }

    ICanvasImage CanvasImage { get; }

    IReadOnlyList<IBitmapFrame> Frames { get; }

    Size SizeInDIPs { get; }

    BitmapSize SizeInPixels { get; }

    ColorSpaceInfo ColorSpace { get; }

}
