using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoVieweApp.Services;

public partial interface IRotatePhotoService
{
    bool CanRotate(BitmapFileInfo photo);

    Task RotateClockwise90DegreesAsync(BitmapFileInfo photo);
}

internal class RotatePhotoService : IRotatePhotoService
{
    private readonly IMetadataService metadataService;

    public RotatePhotoService(IMetadataService metadataService)
    {
        this.metadataService = metadataService;
    }

    public bool CanRotate(BitmapFileInfo photo)
    {
        string fileExtension = photo.File.FileType.ToLower();

        if (MetadataProperties.Orientation.IsSupported(fileExtension))
        {
            return true;
        }

        foreach (var codec in BitmapEncoder.GetEncoderInformationEnumerator())
        {
            if (codec.FileExtensions.Contains(fileExtension))
            {
                return true;
            }
        }

        return false;
    }

    public async Task RotateClockwise90DegreesAsync(BitmapFileInfo photo)
    {
        if (MetadataProperties.Orientation.IsSupported(photo.File.FileType.ToLower()))
        {
            await RotateByMetadataAsync(photo);
        }
        else
        {
            await RotateByPixelAsync(photo.File);
        }
    }

    private async Task RotateByMetadataAsync(BitmapFileInfo photo)
    {
        PhotoOrientation orientation = await metadataService.GetMetadataAsync(photo, MetadataProperties.Orientation).ConfigureAwait(false);

        switch (orientation)
        {
            case PhotoOrientation.Normal:
            case PhotoOrientation.Unspecified:
                orientation = PhotoOrientation.Rotate270;
                break;
            case PhotoOrientation.Rotate270:
                orientation = PhotoOrientation.Rotate180;
                break;
            case PhotoOrientation.Rotate180:
                orientation = PhotoOrientation.Rotate90;
                break;
            case PhotoOrientation.Rotate90:
                orientation = PhotoOrientation.Normal;
                break;
            default:
                throw new NotSupportedException("Invalid image orientation.");
        }

        await metadataService.WriteMetadataAsync(photo, MetadataProperties.Orientation, orientation).ConfigureAwait(false);
    }

    private static async Task RotateByPixelAsync(IStorageFile file)
    {
        using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite).AsTask().ConfigureAwait(false),
                                   memoryStream = new InMemoryRandomAccessStream())
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream).AsTask().ConfigureAwait(false);
            BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(memoryStream, decoder).AsTask().ConfigureAwait(false);
            encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;
            await encoder.FlushAsync().AsTask().ConfigureAwait(false);
            memoryStream.Seek(0);
            fileStream.Seek(0);
            fileStream.Size = 0;
            await RandomAccessStream.CopyAsync(memoryStream, fileStream).AsTask().ConfigureAwait(false);
        }
    }
}
