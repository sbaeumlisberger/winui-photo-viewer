using Microsoft.Graphics.Canvas;
using Windows.Foundation;

namespace PhotoViewerApp.Models;

public interface IBitmapFrame : IDisposable
{
    ICanvasImage CanvasImage { get; }

    Point Offset { get; }

    double Delay { get; }

    bool RequiresClear { get; }
}
