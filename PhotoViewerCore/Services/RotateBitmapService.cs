using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewerApp.Services;

public partial interface IRotateBitmapService
{
    bool CanRotate(IBitmapFileInfo photo);

    Task RotateClockwise90DegreesAsync(IBitmapFileInfo photo);
}

internal class RotateBitmapService : IRotateBitmapService
{
    private readonly IMetadataService metadataService;

    public RotateBitmapService(IMetadataService metadataService)
    {
        this.metadataService = metadataService;
    }

    public bool CanRotate(IBitmapFileInfo photo)
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

    public async Task RotateClockwise90DegreesAsync(IBitmapFileInfo photo)
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

    private async Task RotateByMetadataAsync(IBitmapFileInfo photo)
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
