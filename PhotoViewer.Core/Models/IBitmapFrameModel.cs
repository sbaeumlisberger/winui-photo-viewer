using Microsoft.Graphics.Canvas;
using Windows.Foundation;

namespace PhotoViewer.App.Models;

public interface IBitmapFrameModel : IDisposable
{
    ICanvasImage CanvasImage { get; }

    Point Offset { get; }

    double Delay { get; }

    bool RequiresClear { get; }
}
