using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using Windows.Graphics;
using Windows.Storage;
using Xunit;
using Xunit.Abstractions;

namespace PhotoViewer.Test.Services;

public class CropImageServiceTest
{
    private readonly CropImageService cropImageService = new CropImageService(new StrongReferenceMessenger(), Substitute.For<IMetadataService>());

    private readonly string TestFolder = TestUtils.CreateTestFolder();

    public CropImageServiceTest(ITestOutputHelper output)
    {
        output.WriteLine("TestFolder: " + TestFolder);
    }

    [Fact]
    public async Task CropInPlaceWorks()
    {
        var storageFile = await GetTestFileAsync();
        var imageFile = new BitmapFileInfo(storageFile);
        ulong orginalFileSize = await GetFileSize(storageFile);

        var newBounds = new RectInt32(200, 200, 600, 400);
        await cropImageService.CropImageAsync(imageFile, newBounds);

        ulong newFileSize = await GetFileSize(storageFile);
        Assert.True(newFileSize < orginalFileSize);
        var imageProperties = await storageFile.Properties.GetImagePropertiesAsync();
        Assert.Equal(newBounds.Width, (int)imageProperties.Width);
        Assert.Equal(newBounds.Height, (int)imageProperties.Height);
    }

    [Fact]
    public async Task CropToCopyWorks()
    {
        var storageFile = await GetTestFileAsync();
        var imageFile = new BitmapFileInfo(storageFile);
        ulong orginalFileSize = await GetFileSize(storageFile);
        var copyFilePath = Path.Combine(TestFolder, "Copy.jpg");
        using var copyStream = File.Create(copyFilePath);

        var newBounds = new RectInt32(200, 200, 600, 400);
        await cropImageService.CropImageAsync(imageFile, newBounds, copyStream);
        copyStream.Close();

        var copy = await StorageFile.GetFileFromPathAsync(copyFilePath);
        ulong newFileSize = await GetFileSize(copy);
        Assert.True(newFileSize < orginalFileSize);
        var imageProperties = await copy.Properties.GetImagePropertiesAsync();
        Assert.Equal(newBounds.Width, (int)imageProperties.Width);
        Assert.Equal(newBounds.Height, (int)imageProperties.Height);
    }

    [Fact]
    public async Task ThrowsForInvalidBounds()
    {
        var storageFile = await GetTestFileAsync();
        var imageFile = new BitmapFileInfo(storageFile);

        RectInt32 invalidBounds = new RectInt32(0, 0, 0, 0);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => cropImageService.CropImageAsync(imageFile, invalidBounds));
    }

    // TODO assert if offset is correct

    // TODO test update of people tags 

    private async Task<StorageFile> GetTestFileAsync()
    {
        string srcPath = Path.Combine(Environment.CurrentDirectory, "Resources", "CropImageServiceTest/TestFile.jpg");
        string filePath = Path.Combine(TestFolder, "TestFile.jpg");
        File.Copy(srcPath, filePath, true);
        return await StorageFile.GetFileFromPathAsync(filePath);
    }

    private async Task<ulong> GetFileSize(IStorageFile storageFile)
    {
        using var fileStream = await storageFile.OpenAsync(FileAccessMode.Read);
        return fileStream.Size;
    }

}
