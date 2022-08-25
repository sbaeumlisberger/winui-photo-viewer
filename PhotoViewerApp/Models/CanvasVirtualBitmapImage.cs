using System.Collections.Generic;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewerCoreModule.Model
{
    public class CanvasVirtualBitmapImage : IBitmapImage
    {
        public string ID { get; }

        public CanvasDevice Device { get; }

        public CanvasVirtualBitmap CanvasVirtualBitmap { get; }

        public ICanvasImage CanvasImage => CanvasVirtualBitmap;

        public Size SizeInDIPs { get; }

        public BitmapSize SizeInPixels { get; }

        public ColorSpaceInfo ColorSpace { get; }

        public CanvasVirtualBitmapImage(string id, CanvasVirtualBitmap canvasVirtualBitmap, ColorSpaceInfo colorSpace)
        {
            ID = id;
            Device = canvasVirtualBitmap.Device;
            CanvasVirtualBitmap = canvasVirtualBitmap;
            SizeInDIPs = canvasVirtualBitmap.Size;
            SizeInPixels = canvasVirtualBitmap.SizeInPixels;
            ColorSpace = colorSpace;
        }

        public void Dispose()
        {
            CanvasVirtualBitmap.Dispose();
        }
    }
}
