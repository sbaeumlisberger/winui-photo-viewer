using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Drawing;
using WIC;
using Windows.Graphics;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.Services;

public class CropImageServiceTest
{
    private readonly CropImageService cropImageService = new CropImageService(new StrongReferenceMessenger(), Substitute.For<IMetadataService>());

    private string testFolder = "";

    private readonly IWICImagingFactory wic = WICImagingFactory.Create();

    private const int CropOffset = 100;
    private const int CropWidth = 900;
    private const int CropHeight = 500;

    private static readonly Color BackgroundColor = Color.LightGray;

    public record TestData(
        string FileName,
        RectInt32 CropRect,
        ExpectedCornerColors ExpectedCornerColors,
        string ExpectedPeopleTagName,
        double ExpectedPeopleTagX,
        double ExpectedPeopleTagY);

    public record ExpectedCornerColors(
        Color TopLeft,
        Color TopRight,
        Color BottomLeft,
        Color BottomRight);

    public static TheoryData<TestData> GetTestData()
    {
        return [
            new TestData(
                "TestFile.jpg",
                new RectInt32(CropOffset, CropOffset, CropWidth, CropHeight),
                new ExpectedCornerColors(Color.Red, BackgroundColor, BackgroundColor, BackgroundColor),
                "RedSquare", 0, 0),

            new TestData(
                "TestFile_Rotate90.jpg",
                new RectInt32(CropOffset, CropOffset, CropHeight, CropWidth),
                new ExpectedCornerColors(BackgroundColor, Color.Green, BackgroundColor, BackgroundColor),
                "GreenSquare", 0.95, 0),

            new TestData("TestFile_Rotate180.jpg",
                new RectInt32(CropOffset, CropOffset, CropWidth, CropHeight),
                new ExpectedCornerColors(BackgroundColor, BackgroundColor, BackgroundColor, Color.Yellow),
                "YellowSquare", 0.95, 0.9),

            new TestData("TestFile_Rotate270.jpg",
                new RectInt32(CropOffset, CropOffset, CropHeight, CropWidth),
                new ExpectedCornerColors(BackgroundColor, BackgroundColor, Color.Blue, BackgroundColor),
                "BlueSquare", 0, 0.9)
        ];
    }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task CropJpgInPlace(TestData testData)
    {
        testFolder = TestUtils.CreateTestFolder(nameof(CropImageServiceTest), nameof(CropJpgInPlace));
        var storageFile = await GetCopiedTestFileAsync(testData.FileName);
        var imageFile = new BitmapFileInfo(storageFile);
        long originalFileSize = new FileInfo(imageFile.FilePath).Length;

        await cropImageService.CropImageAsync(imageFile, testData.CropRect);

        AssertCrop(imageFile.FilePath, originalFileSize, testData.ExpectedCornerColors);
    }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task CropJpgToCopy(TestData testData)
    {
        testFolder = TestUtils.CreateTestFolder(nameof(CropImageServiceTest), nameof(CropJpgToCopy));
        var storageFile = await GetTestFileAsync(testData.FileName);
        var imageFile = new BitmapFileInfo(storageFile);
        long originalFileSize = new FileInfo(imageFile.FilePath).Length; ;
        var copyFilePath = Path.Combine(testFolder, testData.FileName.Replace(".jpg", "_Copy.jpg"));

        using (var copyStream = File.Create(copyFilePath))
        {
            await cropImageService.CropImageAsync(imageFile, testData.CropRect, copyStream);
        }

        AssertCrop(copyFilePath, originalFileSize, testData.ExpectedCornerColors);
    }

    [Fact]
    public async Task CropPng()
    {
        testFolder = TestUtils.CreateTestFolder(nameof(CropImageServiceTest), nameof(CropPng));
        var storageFile = await GetCopiedTestFileAsync("TestFile.png");
        var imageFile = new BitmapFileInfo(storageFile);
        long originalFileSize = new FileInfo(imageFile.FilePath).Length;

        var newBounds = new RectInt32(CropOffset, CropOffset, CropWidth, CropHeight);
        await cropImageService.CropImageAsync(imageFile, newBounds);

        var expectedCornerColors = new ExpectedCornerColors(Color.Red, BackgroundColor, BackgroundColor, BackgroundColor);
        AssertCrop(imageFile.FilePath, originalFileSize, expectedCornerColors, assertThumbnail: false);
    }

    [Fact]
    public async Task ThrowsForInvalidBounds()
    {
        var storageFile = await GetTestFileAsync("TestFile.jpg");
        var imageFile = new BitmapFileInfo(storageFile);

        RectInt32 invalidBounds = new RectInt32(0, 0, 0, 0);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => cropImageService.CropImageAsync(imageFile, invalidBounds));
    }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task PeopleTagsShouldBeUpdated(TestData testData)
    {
        testFolder = TestUtils.CreateTestFolder(nameof(CropImageServiceTest), nameof(PeopleTagsShouldBeUpdated));
        var storageFile = await GetCopiedTestFileAsync(testData.FileName);
        var imageFile = new BitmapFileInfo(storageFile);

        await cropImageService.CropImageAsync(imageFile, testData.CropRect);

        using var fileStream = File.Open(imageFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
        var metadataReader = new MetadataReader(decoder.GetFrame(0).GetMetadataQueryReader(), decoder.GetDecoderInfo());
        var peopleTags = metadataReader.GetProperty(MetadataProperties.People);

        Assert.Single(peopleTags);
        Assert.Equal(testData.ExpectedPeopleTagName, peopleTags[0].Name);
        Assert.Equal(testData.ExpectedPeopleTagX, peopleTags[0].Rectangle!.Value.X, 0.01);
        Assert.Equal(testData.ExpectedPeopleTagY, peopleTags[0].Rectangle!.Value.Y, 0.01);
    }

    private void AssertCrop(string filePath, long originalFileSize, ExpectedCornerColors expectedCornerColors, bool assertThumbnail = true)
    {
        long newFileSize = new FileInfo(filePath).Length;
        Assert.True(newFileSize < originalFileSize);

        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

        AssertImageData(decoder.GetFrame(0), CropWidth, CropHeight, expectedCornerColors);

        if (assertThumbnail)
        {
            AssertImageData(decoder.GetFrame(0).GetThumbnail(), 256, 142, expectedCornerColors);
        }
    }

    private void AssertImageData(IWICBitmapSource image, int expectedWidth, int expectedHeight, ExpectedCornerColors expectedCornerColors)
    {
        var imageSize = image.GetSize();
        var imageData = BytesToColorGrid(image.GetPixels(), imageSize.Width, imageSize.Height);
        Assert.Equal(expectedWidth, imageSize.Width);
        Assert.Equal(expectedHeight, imageSize.Height);
        Assert.Equal(expectedCornerColors.TopLeft, imageData[0, 0], CompareColors);
        Assert.Equal(expectedCornerColors.TopRight, imageData[0, imageSize.Width - 1], CompareColors);
        Assert.Equal(expectedCornerColors.BottomLeft, imageData[imageSize.Height - 1, 0], CompareColors);
        Assert.Equal(expectedCornerColors.BottomRight, imageData[imageSize.Height - 1, imageSize.Width - 1], CompareColors);
    }

    private bool CompareColors(Color colorA, Color colorB)
    {
        const int tolerance = 3;
        return Math.Abs(colorA.R - colorB.R) <= tolerance
            && Math.Abs(colorA.G - colorB.G) <= tolerance
            && Math.Abs(colorA.B - colorB.B) <= tolerance;
    }

    private async Task<StorageFile> GetCopiedTestFileAsync(string fileName)
    {
        string srcPath = TestUtils.GetResourceFile("CropImageServiceTest", fileName);
        string copyPath = Path.Combine(testFolder, fileName);
        File.Copy(srcPath, copyPath, true);
        return await StorageFile.GetFileFromPathAsync(copyPath);
    }

    private async Task<StorageFile> GetTestFileAsync(string fileName)
    {
        return await StorageFile.GetFileFromPathAsync(TestUtils.GetResourceFile("CropImageServiceTest", fileName));
    }

    private Color[,] BytesToColorGrid(byte[] bgrBytes, int width, int height)
    {
        if (bgrBytes.Length != width * height * 3)
        {
            throw new ArgumentException("Byte array size does not match width * height * 3.");
        }

        var colors = new Color[height, width];

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte b = bgrBytes[index++];
                byte g = bgrBytes[index++];
                byte r = bgrBytes[index++];
                colors[y, x] = Color.FromArgb(255, r, g, b);
            }
        }

        return colors;
    }
}
