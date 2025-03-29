using Essentials.NET;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Imaging;

namespace PhotoViewer.Core.Models;

public class CanvasBitmapImageModel : ShareableDisposable, ICanvasBitmapImageModel
{
    public string ID { get; }

    public CanvasDevice Device { get; }

    public CanvasBitmap CanvasBitmap { get; }

    public ICanvasImage CanvasImage => CanvasBitmap;

    public IReadOnlyList<IBitmapFrameModel> Frames { get; }

    public BitmapSize SizeInPixels { get; }

    public ColorSpaceInfo ColorSpace { get; }

    public CanvasBitmapImageModel(string id, CanvasBitmap canvasBitmap, ColorSpaceInfo colorSpace)
    {
        ID = id;
        Device = canvasBitmap.Device;
        CanvasBitmap = canvasBitmap;
        Frames = Array.Empty<IBitmapFrameModel>();
        SizeInPixels = canvasBitmap.SizeInPixels;
        ColorSpace = colorSpace;
    }

    public CanvasBitmapImageModel(string id, CanvasDevice device, IReadOnlyList<CanvasBitmapFrameModel> frames, ColorSpaceInfo colorSpace)
    {
        ID = id;
        Device = device;
        CanvasBitmap = frames[0].CanvasBitmap;
        Frames = frames;
        SizeInPixels = frames[0].CanvasBitmap.SizeInPixels;
        ColorSpace = colorSpace;
    }

    protected override void OnDispose()
    {
        CanvasBitmap.Dispose();
    }

    public override string ToString()
    {
        return nameof(CanvasBitmapImageModel) + ":" + ID;
    }
}
