using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using Windows.Graphics.Imaging;
using Xunit;

namespace PhotoViewer.Test.Services;

public class FaceDetectionServiceTest
{

    [Fact]
    public async Task DetectFacesAsync()
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "Resources", "FaceDetectionServiceTest", "TestImage.jpg");
        var iamgeLoadService = new ImageLoaderService(new GifImageLoaderService());
        var image = (ICanvasBitmapImageModel)await iamgeLoadService.LoadFromFileAsync(filePath, default);

        var faceDetectionService = new FaceDetectionService();
        var detectedFaces = await faceDetectionService.DetectFacesAsync(image, default);

        Assert.Equal(4, detectedFaces.Count);
        AssertFaceBox(new BitmapBounds(313, 376, 531, 765), detectedFaces[0].FaceBox, 20);
        AssertFaceBox(new BitmapBounds(1297, 462, 540, 750), detectedFaces[1].FaceBox, 20);
        AssertFaceBox(new BitmapBounds(2322, 532, 499, 694), detectedFaces[2].FaceBox, 20);
        AssertFaceBox(new BitmapBounds(3181, 469, 484, 657), detectedFaces[3].FaceBox, 20);
    }

    private static void AssertFaceBox(BitmapBounds expected, BitmapBounds actual, uint tolerance)
    {
        Assert.InRange(actual.X, expected.X - tolerance, expected.X + tolerance);
        Assert.InRange(actual.Y, expected.Y - tolerance, expected.Y + tolerance);
        Assert.InRange(actual.Width, expected.Width - tolerance, expected.Width + tolerance);
        Assert.InRange(actual.Height, expected.Height - tolerance, expected.Height + tolerance);
    }

}
