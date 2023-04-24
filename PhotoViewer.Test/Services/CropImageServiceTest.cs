using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.Services;

public class CropImageServiceTest
{
    // TODO

    private readonly CropImageService cropImageService = new CropImageService();

    [Fact]
    public async Task DoesWorkAtAll() 
    {
        var storageFile = await StorageFile.GetFileFromPathAsync(Path.Combine(Environment.CurrentDirectory, "Resources", "CropImageServiceTest.jpg"));
        var imageFile = new BitmapFileInfo(storageFile);

        var newBounds = new Rect(200, 200, 400, 400);
        await cropImageService.CropImageAsync(imageFile, newBounds);

    }

}
