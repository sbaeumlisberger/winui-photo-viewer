using Microsoft.Graphics.Canvas;
using PhotoViewer.App.Exceptions;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Runtime.InteropServices;
using System.Text;
using WIC;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.App.Services;

public interface IImageLoaderService
{
    Task<IBitmapImage> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken);
}

public class ImageLoaderService : IImageLoaderService
{
    private CanvasDevice Device => CanvasDevice.GetSharedDevice();

    private IGifImageLoaderService gifImageLoaderService;

    public ImageLoaderService(IGifImageLoaderService gifImageLoaderService)
    {
        this.gifImageLoaderService = gifImageLoaderService;
    }

    public async Task<IBitmapImage> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken)
    {
        try
        {
            if (file.FileExtension.ToLower() == ".gif")
            {
                return await LoadGifAsync(file, cancellationToken).ConfigureAwait(false);
            }
            return await LoadBitmapAsync(file, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (TryGetDeocderInfoForFileExtension(file.FileExtension) is null)
            {
                throw new CodecNotFoundException(file.FileExtension, ex);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task<IBitmapImage> LoadGifAsync(IBitmapFileInfo file, CancellationToken cancellationToken)
    {
        using (var fileStream = await file.OpenAsync(FileAccessMode.Read).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await gifImageLoaderService.LoadAsync(file.DisplayName, Device, fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IBitmapImage> LoadBitmapAsync(IBitmapFileInfo file, CancellationToken cancellationToken)
    {
        using (var fileStream = await file.OpenAsync(FileAccessMode.Read).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ColorSpaceInfo colorSpace = GetColorSpaceInfo(fileStream);
            fileStream.Seek(0);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var canvasBitmap = await CanvasBitmap.LoadAsync(Device, fileStream).AsTask(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new PVBitmapImage(file.DisplayName, canvasBitmap, colorSpace);
            }
            catch (ArgumentException ex) when (ex.HResult == -2147024809)
            {
                var canvasVirtualBitmap = await CanvasVirtualBitmap.LoadAsync(Device, fileStream).AsTask(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new PVVirtualBitmapImage(file.DisplayName, canvasVirtualBitmap, colorSpace);
            }
        }
    }

    private ColorSpaceInfo GetColorSpaceInfo(IRandomAccessStream fileStream)
    {
        var wic = new WICImagingFactory();

        var decoder = wic.CreateDecoderFromStream(fileStream.AsStream(), WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

        var colorContexts = GetColorContexts(decoder);

        byte[]? colorProfile = null;

        if (colorContexts.FirstOrDefault(cc => cc.GetType() == WICColorContextType.WICColorContextProfile) is IWICColorContext colorContext)
        {
            colorProfile = colorContext.GetProfileBytes();
        }

        ColorSpaceType colorSpaceType = GetColorSpaceType(colorContexts, colorProfile);

        return new ColorSpaceInfo(colorSpaceType, colorProfile);
    }

    private IWICColorContext[] GetColorContexts(IWICBitmapDecoder decoder)
    {
        try
        {
            return decoder.GetFrame(0).GetColorContexts();
        }
        catch (COMException ex) when (ex.HResult == WinCodecError.UNSUPPORTED_OPERATION)
        {
            return Array.Empty<IWICColorContext>();
        }
    }

    private ColorSpaceType GetColorSpaceType(IWICColorContext[] colorContexts, byte[]? colorProfile)
    {
        ExifColorSpace? exifColorSpace = colorContexts
               .FirstOrDefault(cc => cc.GetType() == WICColorContextType.WICColorContextExifColorSpace)
               ?.GetExifColorSpace();

        if (exifColorSpace == ExifColorSpace.SRGB || exifColorSpace == /*ExifColorSpace.Uncalibrated*/ (ExifColorSpace)65535)
        {
            return ColorSpaceType.SRGB;
        }
        else if (exifColorSpace == ExifColorSpace.AdobeRGB ||
            (colorProfile != null && Encoding.ASCII.GetString(colorProfile).Contains("Adobe RGB")))
        {
            return ColorSpaceType.AdobeRGB;
        }
        else if (colorProfile != null)
        {
            return ColorSpaceType.Unknown;
        }
        return ColorSpaceType.NotSpecified;
    }

    private static BitmapCodecInformation? TryGetDeocderInfoForFileExtension(string fileExtension)
    {
        var installedEncoders = BitmapDecoder.GetDecoderInformationEnumerator();
        foreach (var encoderInfo in installedEncoders)
        {
            if (encoderInfo.FileExtensions.Contains(fileExtension.ToLower()))
            {
                return encoderInfo;
            }
        }
        return null;
    }

}
