using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using WIC;
using Windows.Foundation;
using Windows.Graphics;

namespace PhotoViewer.Core.Services;

public interface ICropImageService
{
    Task CropImageAsync(IBitmapFileInfo imageFile, RectInt32 newBounds, Stream? dstStream = null);
}

internal class CropImageService : ICropImageService
{
    private readonly IWICImagingFactory wic = WICImagingFactory.Create();

    private readonly IMessenger messenger;

    private readonly IMetadataService metadataService;

    public CropImageService(IMessenger messenger, IMetadataService metadataService)
    {
        this.messenger = messenger;
        this.metadataService = metadataService;
    }

    public async Task CropImageAsync(IBitmapFileInfo bitmapFile, RectInt32 newBounds, Stream? dstStream = null)
    {
        if (newBounds.Width < 1 || newBounds.Height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(newBounds));
        }

        if (string.IsNullOrEmpty(bitmapFile.FilePath))
        {
            throw new Exception("File not writeable (no file path)");
        }

        var fileAccess = dstStream is null ? FileAccess.ReadWrite : FileAccess.Read;
        using var fileStream = File.Open(bitmapFile.FilePath, FileMode.Open, fileAccess);

        var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

        if (dstStream is null)
        {
            using var memoryStream = new MemoryStream();

            bool peopleTagsUpdated = await EncodeAsync(bitmapFile, decoder, memoryStream, newBounds).ConfigureAwait(false);

            memoryStream.Position = 0;
            fileStream.Position = 0;
            fileStream.SetLength(0);
            await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
            await fileStream.FlushAsync().ConfigureAwait(false);
            fileStream.Close();

            bitmapFile.InvalidateCache();

            messenger.Send(new BitmapModifiedMesssage(bitmapFile));

            if (peopleTagsUpdated)
            {
                messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.People));
            }
        }
        else
        {
            dstStream.SetLength(0);
            await EncodeAsync(bitmapFile, decoder, dstStream, newBounds).ConfigureAwait(false);
            await dstStream.FlushAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> EncodeAsync(IBitmapFileInfo bitmapFile, IWICBitmapDecoder decoder, Stream dstStream, RectInt32 newBounds)
    {
        var srcFrame = decoder.GetFrame(0);

        var metadataReader = new MetadataReader(srcFrame.GetMetadataQueryReader(), decoder.GetDecoderInfo());

        var orientation = MetadataProperties.Orientation.SupportedFormats.Contains(decoder.GetContainerFormat())
            ? metadataReader.GetProperty(MetadataProperties.Orientation)
            : PhotoOrientation.Normal;

        var newBoundsUnrotated = GetUnrotatedRect(newBounds, orientation, srcFrame.GetSize());

        var encoder = wic.CreateEncoder(decoder.GetContainerFormat());
        encoder.Initialize(dstStream.AsCOMStream(), WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);

        var frame = encoder.CreateNewFrame();
        frame.Initialize();
        frame.SetSize(newBoundsUnrotated.Width, newBoundsUnrotated.Height);
        frame.SetResolution(srcFrame.GetResolution());
        frame.SetPixelFormat(srcFrame.GetPixelFormat());

        frame.AsMetadataBlockWriter().InitializeFromBlockReader(srcFrame.AsMetadataBlockReader());

        var metadataWriter = new MetadataWriter(frame.GetMetadataQueryWriter(), encoder.GetEncoderInfo());

        bool peopleTagsUpdated = false;
        if (MetadataProperties.People.SupportedFormats.Contains(decoder.GetContainerFormat()))
        {
            peopleTagsUpdated = UpdatePeopleTags(bitmapFile, metadataReader, metadataWriter, srcFrame.GetSize(), newBoundsUnrotated);
        }

        try
        {
            frame.SetThumbnail(GenerateThumbnail(srcFrame, newBoundsUnrotated));
        }
        catch (Exception e) when (e.HResult == WinCodecError.UNSUPPORTED_OPERATION)
        {
            Log.Debug($"Embedded thumbnail not supported by {bitmapFile.FilePath}");
        }

        frame.WriteSource(srcFrame, newBoundsUnrotated);

        frame.Commit();
        encoder.Commit();
        await dstStream.FlushAsync().ConfigureAwait(false);

        return peopleTagsUpdated;
    }

    private IWICBitmapSource GenerateThumbnail(IWICBitmapFrameDecode srcFrame, WICRect newBoundsUnrotated)
    {
        double aspectRadio = newBoundsUnrotated.Width / (double)newBoundsUnrotated.Height;
        int thumbnailWidth = (int)(256 * Math.Min(aspectRadio, 1));
        int thumbnailHeight = (int)(256 / Math.Max(aspectRadio, 1));
        var clipper = wic.CreateBitmapClipper();
        clipper.Initialize(srcFrame, newBoundsUnrotated);
        var scaler = wic.CreateBitmapScaler();
        scaler.Initialize(clipper, thumbnailWidth, thumbnailHeight, WICBitmapInterpolationMode.WICBitmapInterpolationModeFant);
        return scaler;
    }

    private bool UpdatePeopleTags(IBitmapFileInfo bitmapFile, MetadataReader metadataReader, MetadataWriter metadataWriter, WICSize sizeBefore, WICRect newBounds)
    {
        var peopleTags = metadataReader.GetProperty(MetadataProperties.People);

        if (peopleTags.Any() is false)
        {
            return false;
        }

        for (int i = peopleTags.Count - 1; i >= 0; i--)
        {
            PeopleTag peopleTag = peopleTags[i];

            if (peopleTag.Rectangle is null)
            {
                continue;
            }

            Rect newBoundsRect = new Rect(newBounds.X, newBounds.Y, newBounds.Width, newBounds.Height);

            Rect peopleTagRectInPixels = new Rect(
                peopleTag.Rectangle.Value.X * sizeBefore.Width,
                peopleTag.Rectangle.Value.Y * sizeBefore.Height,
                peopleTag.Rectangle.Value.Width * sizeBefore.Width,
                peopleTag.Rectangle.Value.Height * sizeBefore.Height);

            if (peopleTagRectInPixels.Intersects(newBoundsRect))
            {
                peopleTagRectInPixels.Intersect(newBoundsRect);
                var newPeopleTagRectangle = new FaceRect(
                    (peopleTagRectInPixels.X - newBounds.X) / newBounds.Width,
                    (peopleTagRectInPixels.Y - newBounds.Y) / newBounds.Height,
                    peopleTagRectInPixels.Width / newBounds.Width,
                    peopleTagRectInPixels.Height / newBounds.Height);
                peopleTags[i] = new PeopleTag(peopleTag.Name, newPeopleTagRectangle, peopleTag.EmailDigest, peopleTag.LiveCID);
            }
            else
            {
                peopleTags.Remove(peopleTag);
            }
        }

        metadataWriter.SetProperty(MetadataProperties.People, peopleTags);

        metadataService.UpdateCache(bitmapFile, MetadataProperties.People, peopleTags);

        return true;
    }

    private WICRect GetUnrotatedRect(RectInt32 rotatedRect, PhotoOrientation orientation, WICSize imageSize)
    {
        switch (orientation)
        {
            case PhotoOrientation.Normal:
            case PhotoOrientation.Unspecified:
                return new WICRect()
                {
                    X = rotatedRect.X,
                    Y = rotatedRect.Y,
                    Width = rotatedRect.Width,
                    Height = rotatedRect.Height
                };
            case PhotoOrientation.Rotate270:
                return new WICRect()
                {
                    X = rotatedRect.Y,
                    Y = imageSize.Height - rotatedRect.X - rotatedRect.Width,
                    Width = rotatedRect.Height,
                    Height = rotatedRect.Width
                };
            case PhotoOrientation.Rotate180:
                return new WICRect()
                {
                    X = imageSize.Width - rotatedRect.X - rotatedRect.Width,
                    Y = imageSize.Height - rotatedRect.Y - rotatedRect.Height,
                    Width = rotatedRect.Width,
                    Height = rotatedRect.Height
                };
            case PhotoOrientation.Rotate90:
                return new WICRect()
                {
                    X = imageSize.Width - rotatedRect.Y - rotatedRect.Height,
                    Y = rotatedRect.X,
                    Width = rotatedRect.Height,
                    Height = rotatedRect.Width
                };
            default:
                throw new NotSupportedException("Invalid image orientation.");
        }
    }
}
