using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewer.App.Models;

public class PVBitmapImage : IBitmapImage
{
    public string ID { get; }

    public CanvasDevice Device { get; }

    public CanvasBitmap CanvasBitmap { get; }

    public ICanvasImage CanvasImage => CanvasBitmap;

    public IReadOnlyList<IBitmapFrame> Frames { get; }

    public Size SizeInDIPs { get; }

    public BitmapSize SizeInPixels { get; }

    public ColorSpaceInfo ColorSpace { get; }

    public PVBitmapImage(string id, CanvasBitmap canvasBitmap, ColorSpaceInfo colorSpace)
    {
        ID = id;
        Device = canvasBitmap.Device;
        CanvasBitmap = canvasBitmap;
        Frames = Array.Empty<IBitmapFrame>();
        SizeInDIPs = canvasBitmap.Size;
        SizeInPixels = canvasBitmap.SizeInPixels;
        ColorSpace = colorSpace;
    }

    public PVBitmapImage(string id, CanvasDevice device, IReadOnlyList<PVBitmapFrame> frames, ColorSpaceInfo colorSpace)
    {
        ID = id;
        Device = device;
        CanvasBitmap = frames[0].CanvasBitmap;
        Frames = frames;
        SizeInDIPs = frames[0].CanvasBitmap.Size;
        SizeInPixels = frames[0].CanvasBitmap.SizeInPixels;
        ColorSpace = colorSpace;
    }

    public void Dispose()
    {
        CanvasBitmap.Dispose();
    }
}
