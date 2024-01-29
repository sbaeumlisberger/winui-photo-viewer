using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Services;
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
    private readonly WICImagingFactory wic = new WICImagingFactory();

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
        var newBoundsWIC = ToWICRect(newBounds);

        var srcFrame = decoder.GetFrame(0);

        var encoder = wic.CreateEncoder(decoder.GetContainerFormat());
        encoder.Initialize(dstStream.AsCOMStream(), WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);

        var frame = encoder.CreateNewFrame();
        frame.Initialize();
        frame.SetSize(newBoundsWIC.Width, newBoundsWIC.Height);
        frame.SetResolution(srcFrame.GetResolution());
        frame.SetPixelFormat(srcFrame.GetPixelFormat());

        frame.AsMetadataBlockWriter().InitializeFromBlockReader(srcFrame.AsMetadataBlockReader());

        var metadataReader = new MetadataReader(srcFrame.GetMetadataQueryReader(), decoder.GetDecoderInfo());
        var metadataWriter = new MetadataWriter(frame.GetMetadataQueryWriter(), encoder.GetEncoderInfo());

        bool peopleTagsUpdated = false;
        var mimeTypes = encoder.GetEncoderInfo().GetMimeTypes();
        if (mimeTypes.Contains("image/jpeg") || mimeTypes.Contains("image/tiff"))
        {
            peopleTagsUpdated = UpdatePeopleTags(bitmapFile, metadataReader, metadataWriter, srcFrame.GetSize(), newBounds);
        }

        frame.WriteSource(srcFrame, newBoundsWIC);

        frame.Commit();
        encoder.Commit();
        await dstStream.FlushAsync().ConfigureAwait(false);

        return peopleTagsUpdated;
    }

    private bool UpdatePeopleTags(IBitmapFileInfo bitmapFile, MetadataReader metadataReader, MetadataWriter metadataWriter, WICSize sizeBefore, RectInt32 newBounds)
    {
        var people = metadataReader.GetProperty(MetadataProperties.People);

        if (people.Any() is false)
        {
            return false;
        }

        for (int i = people.Count - 1; i >= 0; i--)
        {
            PeopleTag person = people[i];

            if (person.Rectangle.IsEmpty)
            {
                continue;
            }

            Rect newBoundsRect = new Rect(newBounds.X, newBounds.Y, newBounds.Width, newBounds.Height);

            Rect personRectInPixels = new Rect(
                person.Rectangle.X * sizeBefore.Width,
                person.Rectangle.Y * sizeBefore.Height,
                person.Rectangle.Width * sizeBefore.Width,
                person.Rectangle.Height * sizeBefore.Height);

            if (personRectInPixels.Intersects(newBoundsRect))
            {
                personRectInPixels.Intersect(newBoundsRect);
                person.Rectangle = new FaceRect(
                    (personRectInPixels.X - newBounds.X) / newBounds.Width,
                    (personRectInPixels.Y - newBounds.Y) / newBounds.Height,
                    personRectInPixels.Width / newBounds.Width,
                    personRectInPixels.Height / newBounds.Height);
            }
            else
            {
                people.Remove(person);
            }
        }

        metadataWriter.SetProperty(MetadataProperties.People, people);

        metadataService.UpdateCache(bitmapFile, MetadataProperties.People, people);

        return true;
    }

    private WICRect ToWICRect(RectInt32 rect)
    {
        return new WICRect()
        {
            X = rect.X,
            Y = rect.Y,
            Width = rect.Width,
            Height = rect.Height
        };
    }
}
