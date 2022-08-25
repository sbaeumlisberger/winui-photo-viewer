using System;
using System.Collections.Generic;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewerCoreModule.Model
{
    public interface IBitmapImage : IDisposable
    {
        string ID { get; }

        CanvasDevice Device { get; }

        ICanvasImage CanvasImage { get; }

        Size SizeInDIPs { get; }

        BitmapSize SizeInPixels { get; }

        ColorSpaceInfo ColorSpace { get; }
 
    }
}
