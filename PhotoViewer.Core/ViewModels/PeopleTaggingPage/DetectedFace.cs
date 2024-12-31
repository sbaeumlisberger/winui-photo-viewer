using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Models;
using Windows.Foundation;

namespace PhotoViewer.Core.ViewModels;

public record DetectedFace(
    Rect FaceRectInPercent,
    ICanvasImage FaceImage,
    IBitmapFileInfo File,
    CanvasBitmap SourceImage);
