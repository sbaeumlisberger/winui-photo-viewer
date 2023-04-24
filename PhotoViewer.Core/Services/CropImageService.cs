using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using WIC;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.Services;

internal interface ICropImageService
{
    Task CropImageAsync(IBitmapFileInfo imageFile, Rect newBounds, Stream? dstStream = null);
}

internal class CropImageService : ICropImageService
{
    private readonly WICImagingFactory wic = new WICImagingFactory();

    public async Task CropImageAsync(IBitmapFileInfo imageFile, Rect newBounds, Stream? dstStream = null)
    {
        var newBoundsWIC = new WICRect()
        {
            X = (int)newBounds.X,
            Y = (int)newBounds.Y,
            Width = (int)newBounds.Width,
            Height = (int)newBounds.Height
        };

        using var fileStream = (await imageFile.OpenAsync(dstStream is null ? FileAccessMode.ReadWrite : FileAccessMode.Read).ConfigureAwait(false)).AsStream();

        var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

        var srcFrame = decoder.GetFrame(0);

        if (dstStream is null)
        {
            dstStream = new MemoryStream(); // TODO dispose
        }
        dstStream.SetLength(0);

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

        //metadataWriter.SetMetadata("System.Photo.Orientation", null);

        var mimeTypes = encoder.GetEncoderInfo().GetMimeTypes();
        if (mimeTypes.Contains("image/jpeg") || mimeTypes.Contains("image/tiff"))
        {
            UpdatePeopleTags(metadataReader, metadataWriter, srcFrame.GetSize(), newBounds);
        }

        frame.WriteSource(srcFrame, newBoundsWIC);

        frame.Commit();
        encoder.Commit();
        await dstStream.FlushAsync().ConfigureAwait(false);

        if (dstStream is MemoryStream) 
        {
            dstStream.Position = 0;
            fileStream.Position = 0;
            fileStream.SetLength(0);
            await dstStream.CopyToAsync(fileStream).ConfigureAwait(false);
            await fileStream.FlushAsync().ConfigureAwait(false);
        }
    }

    private void UpdatePeopleTags(MetadataReader metadataReader, MetadataWriter metadataWriter, WICSize sizeBefore, Rect newBounds)
    {
        var people = metadataReader.GetProperty(MetadataProperties.People);

        if (people.Any() is false)
        {
            return;
        }

        for (int i = people.Count - 1; i >= 0; i--)
        {
            PeopleTag person = people[i];
            Rect personRectInPixels = new Rect(
                person.Rectangle.X * sizeBefore.Width,
                person.Rectangle.Y * sizeBefore.Height,
                person.Rectangle.Width * sizeBefore.Width,
                person.Rectangle.Height * sizeBefore.Height);
            if (personRectInPixels.Intersects(newBounds))
            {
                Rect intersection = personRectInPixels.Intersection(newBounds);
                person.Rectangle = new FaceRect(
                    (intersection.X - newBounds.X) / newBounds.Width,
                    (intersection.Y - newBounds.Y) / newBounds.Height,
                    intersection.Width / newBounds.Width,
                    intersection.Height / newBounds.Height);
            }
            else
            {
                people.Remove(person);
            }
        }

        metadataWriter.SetProperty(MetadataProperties.People, people);
    }
}
