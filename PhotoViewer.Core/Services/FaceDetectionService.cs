using Essentials.NET.Logging;
using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Models;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.UI;

namespace PhotoViewer.Core.Services;

internal interface IFaceDetectionService
{
    Task<List<DetectedFaceModel>> DetectFacesAsync(ICanvasBitmapImageModel bitmapImage, CancellationToken cancellationToken = default);
}

internal class FaceDetectionService : IFaceDetectionService
{
    private const int MaxImageHeight = 2000; // larger images are downscaled to improve performance

    private readonly Lazy<Task<FaceDetector>> faceDetector = new Lazy<Task<FaceDetector>>(() => FaceDetector.CreateAsync().AsTask());

    public async Task<List<DetectedFaceModel>> DetectFacesAsync(ICanvasBitmapImageModel bitmapImage, CancellationToken cancellationToken = default)
    {
        if (!FaceDetector.IsSupported)
        {
            Log.Info("Could not detect faces: FaceDetector not supported on this device.");
            return new List<DetectedFaceModel>();
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        var faceDetector = await this.faceDetector.Value.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        float scaleFactor = Math.Min(MaxImageHeight / (float)bitmapImage.CanvasBitmap.SizeInPixels.Height, 1);

        using var softwareBitmap = await ConvertToSoftwareBitmapAsync(bitmapImage.CanvasBitmap, scaleFactor, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        using var softwareBitmapGray8 = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Gray8);
        cancellationToken.ThrowIfCancellationRequested();

        var detectedFaces = await faceDetector.DetectFacesAsync(softwareBitmapGray8).AsTask(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        stopwatch.Stop();
        Log.Debug($"Face detection took {stopwatch.ElapsedMilliseconds} ms");

        return detectedFaces.Select(face => ToDetectedFaceModel(face.FaceBox, 1f / scaleFactor)).OrderBy(face => face.FaceBox.X).ToList();
    }

    private async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(CanvasBitmap canvasBitmap, float scaleFactor, CancellationToken cancellationToken)
    {
        if (scaleFactor != 1)
        {
            using var scaledBitmap = ScaleCanvasBitmap(canvasBitmap, scaleFactor);
            cancellationToken.ThrowIfCancellationRequested();
            return await ConvertToSoftwareBitmapAsync(scaledBitmap, cancellationToken).ConfigureAwait(false);
        }

        return await ConvertToSoftwareBitmapAsync(canvasBitmap, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(CanvasBitmap canvasBitmap, CancellationToken cancellationToken)
    {
        using (var canvasDeviceLock = canvasBitmap.Device.Lock())
        {
            return await SoftwareBitmap.CreateCopyFromSurfaceAsync(canvasBitmap).AsTask(cancellationToken);
        }
    }

    private CanvasRenderTarget ScaleCanvasBitmap(CanvasBitmap canvasBitmap, float scaleFactor)
    {
        float newWidth = canvasBitmap.SizeInPixels.Width * scaleFactor;
        float newHeight = canvasBitmap.SizeInPixels.Height * scaleFactor;

        var device = CanvasDevice.GetSharedDevice();

        var renderTarget = new CanvasRenderTarget(device, newWidth, newHeight, canvasBitmap.Dpi);

        /** TODO: System.AccessViolationException: Attempted to read or write protected memory. 
         * This is often an indication that other memory is corrupt.
         * Stack:
         *  at ABI.System.IDisposable.global::System.IDisposable.Dispose()
         *  at Microsoft.Graphics.Canvas.CanvasDrawingSession.Dispose()
         *  at PhotoViewer.Core.Services.FaceDetectionService.ScaleCanvasBitmap(Microsoft.Graphics.Canvas.CanvasBitmap, Single)
         **/

        using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
        {
            ds.Clear(Color.FromArgb(0, 0, 0, 0));
            ds.DrawImage(canvasBitmap, new Rect(0, 0, newWidth, newHeight));
        }

        return renderTarget;
    }

    private DetectedFaceModel ToDetectedFaceModel(BitmapBounds faceBox, float scaleFactor)
    {
        return new DetectedFaceModel(new BitmapBounds(
            (uint)(faceBox.X * scaleFactor),
            (uint)(faceBox.Y * scaleFactor),
            (uint)(faceBox.Width * scaleFactor),
            (uint)(faceBox.Height * scaleFactor)));
    }
}
