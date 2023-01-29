using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewer.App.Models;

public class PVVirtualBitmapImage : IBitmapImage
{
    public string ID { get; }

    public CanvasDevice Device { get; }

    public CanvasVirtualBitmap CanvasVirtualBitmap { get; }

    public ICanvasImage CanvasImage => CanvasVirtualBitmap;

    public IReadOnlyList<IBitmapFrame> Frames { get; }

    public Size SizeInDIPs { get; }

    public BitmapSize SizeInPixels { get; }

    public ColorSpaceInfo ColorSpace { get; }

    public PVVirtualBitmapImage(string id, CanvasVirtualBitmap canvasVirtualBitmap, ColorSpaceInfo colorSpace)
    {
        ID = id;
        Device = canvasVirtualBitmap.Device;
        CanvasVirtualBitmap = canvasVirtualBitmap;
        Frames = Array.Empty<IBitmapFrame>();
        SizeInDIPs = canvasVirtualBitmap.Size;
        SizeInPixels = canvasVirtualBitmap.SizeInPixels;
        ColorSpace = colorSpace;
    }

    public void Dispose()
    {
        CanvasVirtualBitmap.Dispose();
    }
}
