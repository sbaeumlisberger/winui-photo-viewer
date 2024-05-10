using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Models;
using Windows.Graphics.Imaging;

namespace PhotoViewer.Core.ViewModels;

public record DetectedFace(
    FaceRect FaceRectInPercent,
    BitmapBounds FaceRect,
    ICanvasImage FaceImage,
    IBitmapFileInfo File,
    CanvasBitmap SourceImage,
    string RecognizedName);
