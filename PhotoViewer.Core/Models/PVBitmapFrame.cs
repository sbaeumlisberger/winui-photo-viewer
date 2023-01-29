using Microsoft.Graphics.Canvas;
using Windows.Foundation;

namespace PhotoViewer.App.Models;

public class PVBitmapFrame : IBitmapFrame
{
    public CanvasBitmap CanvasBitmap { get; }

    public ICanvasImage CanvasImage => CanvasBitmap;

    public Point Offset { get; }

    public double Delay { get; }

    public bool RequiresClear { get; }

    public PVBitmapFrame(CanvasBitmap canvasBitmap)
    {
        CanvasBitmap = canvasBitmap;
        Offset = new Point(0, 0);
        Delay = 0;
        RequiresClear = false;
    }

    public PVBitmapFrame(CanvasBitmap canvasBitmap, Point offset, double delay, bool requiresClear)
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
