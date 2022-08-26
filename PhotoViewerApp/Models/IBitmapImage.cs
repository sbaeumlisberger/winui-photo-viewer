using System;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewerApp.Models;

public interface IBitmapImage : IDisposable
{
    string ID { get; }

    CanvasDevice Device { get; }

    ICanvasImage CanvasImage { get; }

    Size SizeInDIPs { get; }

    BitmapSize SizeInPixels { get; }

    ColorSpaceInfo ColorSpace { get; }

}
