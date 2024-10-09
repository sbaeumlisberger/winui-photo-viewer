using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.Services;

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

    public bool CanRotate(IBitmapFileInfo bitmapFile)
    {
        string fileExtension = bitmapFile.FileExtension;

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

    public async Task RotateClockwise90DegreesAsync(IBitmapFileInfo bitmapFile)
    {
        if (MetadataProperties.Orientation.IsSupported(bitmapFile.FileExtension))
        {
            await RotateByMetadataAsync(bitmapFile);
        }
        else
        {
            await RotateByPixelAsync(bitmapFile);
        }
    }

    private async Task RotateByMetadataAsync(IBitmapFileInfo bitmapFile)
    {
        PhotoOrientation orientation = await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.Orientation).ConfigureAwait(false);

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

        await metadataService.WriteMetadataAsync(bitmapFile, MetadataProperties.Orientation, orientation).ConfigureAwait(false);

        bitmapFile.InvalidateCache();
    }

    private static async Task RotateByPixelAsync(IBitmapFileInfo file)
    {
        using (IRandomAccessStream fileStream = await file.OpenAsRandomAccessStreamAsync(FileAccessMode.ReadWrite).ConfigureAwait(false),
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
