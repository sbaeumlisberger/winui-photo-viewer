using Microsoft.Graphics.Canvas;
using Windows.Foundation;

namespace PhotoViewer.Core.Models;

public interface IBitmapFrameModel : IDisposable
{
    ICanvasImage CanvasImage { get; }

    Point Offset { get; }

    TimeSpan Delay { get; }

    bool RequiresClear { get; }
}
