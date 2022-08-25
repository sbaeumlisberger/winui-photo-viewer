using System.Collections.Generic;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewerCoreModule.Model
{
    public class CanvasBitmapImage : IBitmapImage
    {
        public string ID { get; }

        public CanvasDevice Device { get; }

        public CanvasBitmap CanvasBitmap { get; }

        public ICanvasImage CanvasImage => CanvasBitmap;

        public Size SizeInDIPs { get; }

        public BitmapSize SizeInPixels { get; }

        public ColorSpaceInfo ColorSpace { get; }

        public CanvasBitmapImage(string id, CanvasBitmap canvasBitmap, ColorSpaceInfo colorSpace)
        {
            ID = id;
            Device = canvasBitmap.Device;
            CanvasBitmap = canvasBitmap;
            SizeInDIPs = canvasBitmap.Size;
            SizeInPixels = canvasBitmap.SizeInPixels;
            ColorSpace = colorSpace;
        }

        public void Dispose()
        {      
            CanvasBitmap.Dispose();            
        }
    }
}
