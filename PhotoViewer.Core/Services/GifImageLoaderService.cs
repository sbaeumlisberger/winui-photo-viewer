using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Models;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace PhotoViewer.Core.Services;

public interface IGifImageLoaderService
{
    Task<CanvasBitmapImageModel> LoadAsync(string id, CanvasDevice device, Stream stream, CancellationToken? cancellationToken = null);
}

public class GifImageLoaderService : IGifImageLoaderService
{
    private const string LeftPropertyKey = "/imgdesc/Left";
    private const string TopPropertyKey = "/imgdesc/Top";
    private const string DelayPropertyKey = "/grctlext/Delay";
    private const string DisposalPropertyKey = "/grctlext/Disposal";

    public async Task<CanvasBitmapImageModel> LoadAsync(string id, CanvasDevice device, Stream stream, CancellationToken? cancellationToken = null)
    {
        var _cancellationToken = cancellationToken ?? CancellationToken.None;

        var decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.GifDecoderId, stream.AsRandomAccessStream()).AsTask(_cancellationToken).ConfigureAwait(false);
        _cancellationToken.ThrowIfCancellationRequested();

        var frames = new List<CanvasBitmapFrameModel>();
        for (uint index = 0; index < decoder.FrameCount; index++)
        {
            var frame = await decoder.GetFrameAsync(index).AsTask(_cancellationToken).ConfigureAwait(false);
            _cancellationToken.ThrowIfCancellationRequested();

            var softwareBitmap = await frame.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied).AsTask(_cancellationToken).ConfigureAwait(false);
            _cancellationToken.ThrowIfCancellationRequested();
            var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(device, softwareBitmap);

            var bitmapProperties = await frame.BitmapProperties.GetPropertiesAsync([LeftPropertyKey, TopPropertyKey, DelayPropertyKey, DisposalPropertyKey]).AsTask(_cancellationToken).ConfigureAwait(false);
            _cancellationToken.ThrowIfCancellationRequested();

            Point offset = ExtractOffset(bitmapProperties);
            double delayInMilliseconds = ExtractDelay(bitmapProperties);
            bool requiresClear = ExtractRequiresClear(bitmapProperties);

            frames.Add(new CanvasBitmapFrameModel(canvasBitmap, offset, delayInMilliseconds, requiresClear));
        }

        return new CanvasBitmapImageModel(id, device, frames, new ColorSpaceInfo(ColorSpaceType.Unknown));
    }

    private Point ExtractOffset(BitmapPropertySet bitmapProperties)
    {
        var x = (ushort)bitmapProperties[LeftPropertyKey].Value;
        var y = (ushort)bitmapProperties[TopPropertyKey].Value;
        return new Point(x, y);
    }

    private double ExtractDelay(BitmapPropertySet bitmapProperties)
    {
        var delayInMilliseconds = 30.0;
        if (bitmapProperties.TryGetValue(DelayPropertyKey, out var delayProperty) && delayProperty.Type == PropertyType.UInt16)
        {
            var delayInHundredths = (ushort)delayProperty.Value;
            if (delayInHundredths >= 3u) // Prevent degenerate frames with no delay time
            {
                delayInMilliseconds = delayInHundredths * 10.0;
            }
        }
        return delayInMilliseconds;
    }

    private bool ExtractRequiresClear(BitmapPropertySet bitmapProperties)
    {
        bool requiresClear = false;
        if (bitmapProperties.TryGetValue(DisposalPropertyKey, out var disposalProperty) && disposalProperty.Type == PropertyType.UInt8)
        {
            var disposal = (byte)disposalProperty.Value;
            if (disposal == 2)
            {
                requiresClear = true;
            }
        }
        return requiresClear;
    }
}
