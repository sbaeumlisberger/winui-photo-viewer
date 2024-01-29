using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;

namespace PhotoViewer.Core.Services;

internal interface IFaceDetectionService
{
    Task<List<DetectedFaceModel>> DetectFacesAsync(ICanvasBitmapImageModel bitmapImage, CancellationToken cancellationToken = default);
}

internal class FaceDetectionService : IFaceDetectionService
{
    public async Task<List<DetectedFaceModel>> DetectFacesAsync(ICanvasBitmapImageModel bitmapImage, CancellationToken cancellationToken = default)
    {
        if (!FaceDetector.IsSupported)
        {
            Log.Info("Could not detect faces: FaceDetector not supported on this device.");
            return new List<DetectedFaceModel>();
        }

        var faceDetector = await FaceDetector.CreateAsync().AsTask(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(bitmapImage.CanvasBitmap).AsTask(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var softwareBitmapGray8 = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Gray8);

        var detectedFaces = await faceDetector.DetectFacesAsync(softwareBitmapGray8).AsTask(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return detectedFaces.OrderBy(face => face.FaceBox.X).Select(face => new DetectedFaceModel(face.FaceBox)).ToList();
    }
}
