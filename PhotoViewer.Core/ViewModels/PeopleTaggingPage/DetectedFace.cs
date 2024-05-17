using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.ViewModels;

public record DetectedFace(
    FaceRect FaceRectInPercent,
    ICanvasImage FaceImage,
    IBitmapFileInfo File,
    CanvasBitmap SourceImage);
