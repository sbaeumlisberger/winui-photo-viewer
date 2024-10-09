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
        AssertFaceBox(new BitmapBounds(368, 539, 471, 472), detectedFaces[0].FaceBox, 20);
        AssertFaceBox(new BitmapBounds(1339, 630, 478, 478), detectedFaces[1].FaceBox, 20);
        AssertFaceBox(new BitmapBounds(2355, 724, 420, 420), detectedFaces[2].FaceBox, 20);
        AssertFaceBox(new BitmapBounds(3209, 635, 435, 435), detectedFaces[3].FaceBox, 20);
    }

    private static void AssertFaceBox(BitmapBounds expected, BitmapBounds actual, uint tolerance)
    {
        Assert.InRange(actual.X, expected.X - tolerance, expected.X + tolerance);
        Assert.InRange(actual.Y, expected.Y - tolerance, expected.Y + tolerance);
        Assert.InRange(actual.Width, expected.Width - tolerance, expected.Width + tolerance);
        Assert.InRange(actual.Height, expected.Height - tolerance, expected.Height + tolerance);
    }

}
