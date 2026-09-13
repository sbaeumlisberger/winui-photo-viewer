using Microsoft.Graphics.Canvas;
using Windows.Foundation;

namespace PhotoViewer.Core.Models;

public class CanvasBitmapFrameModel : IBitmapFrameModel
{
    public CanvasBitmap CanvasBitmap { get; }

    public ICanvasImage CanvasImage => CanvasBitmap;

    public Point Offset { get; }

    public TimeSpan Delay { get; }

    public bool RequiresClear { get; }

    public CanvasBitmapFrameModel(CanvasBitmap canvasBitmap)
    {
        CanvasBitmap = canvasBitmap;
        Offset = new Point(0, 0);
        Delay = TimeSpan.Zero;
        RequiresClear = false;
    }

    public CanvasBitmapFrameModel(CanvasBitmap canvasBitmap, Point offset, TimeSpan delay, bool requiresClear)
    {
        CanvasBitmap = canvasBitmap;
        Offset = offset;
        Delay = delay;
        RequiresClear = requiresClear;
    }

    public void Dispose()
    {
        CanvasImage.Dispose();
    }
}
