using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using PhotoViewer.Core.Exceptions;
using PhotoViewer.Core.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WIC;
using Windows.Graphics.DirectX;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public interface IImageLoaderService
{
    Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken);

    Task<IBitmapImageModel> LoadFromFileAsync(string filePath, CancellationToken cancellationToken);
}

public class ImageLoaderService : IImageLoaderService
{
    private static CanvasDevice Device => CanvasDevice.GetSharedDevice();

    private readonly IGifImageLoaderService gifImageLoaderService;

    private readonly IWICImagingFactory wic = WICImagingFactory.Create();

    public ImageLoaderService(IGifImageLoaderService gifImageLoaderService)
    {
        this.gifImageLoaderService = gifImageLoaderService;
    }

    public Task<IBitmapImageModel> LoadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    return await LoadGifAsync(filePath, cancellationToken).ConfigureAwait(false);
                }
                return await LoadBitmapAsync(filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Debug($"Failed to load image {Path.GetFileName(filePath)}", ex);

                if (TryGetDeocderInfoForFileExtension(Path.GetExtension(filePath)) is null)
                {
                    throw new CodecNotFoundException(Path.GetExtension(filePath), ex);
                }
                else
                {
                    throw;
                }
            }
        });
    }

    public Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {

                if (file.FileExtension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    return await LoadGifAsync(file, cancellationToken).ConfigureAwait(false);
                }
                return await LoadBitmapAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Debug($"Failed to load image {file.FileName}", ex);

                if (TryGetDeocderInfoForFileExtension(file.FileExtension) is null)
                {
                    throw new CodecNotFoundException(file.FileExtension, ex);
                }
                else
                {
                    throw;
                }
            }
        });
    }
    private async Task<IBitmapImageModel> LoadGifAsync(string filePath, CancellationToken cancellationToken)
    {
        using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await gifImageLoaderService.LoadAsync(Path.GetFileName(filePath), Device, fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IBitmapImageModel> LoadGifAsync(IBitmapFileInfo file, CancellationToken cancellationToken)
    {
        using (var fileStream = await file.OpenAsync(FileAccessMode.Read).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await gifImageLoaderService.LoadAsync(file.DisplayName, Device, fileStream, cancellationToken).ConfigureAwait(false);
        }
    }


    private async Task<IBitmapImageModel> LoadBitmapAsync(string filePath, CancellationToken cancellationToken)
    {
        Stopwatch sw = Stopwatch.StartNew();
        using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            Log.Debug("Stream open took " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();
            return await LoadBitmapAsync(fileStream, Path.GetFileName(filePath), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IBitmapImageModel> LoadBitmapAsync(IBitmapFileInfo file, CancellationToken cancellationToken)
    {
        Stopwatch sw = Stopwatch.StartNew();
        using (var fileStream = await file.OpenAsync(FileAccessMode.Read).ConfigureAwait(false))
        {
            Log.Debug("Stream open took " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();
            return await LoadBitmapAsync(fileStream, file.DisplayName, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IBitmapImageModel> LoadBitmapAsync(Stream fileStream, string displayName, CancellationToken cancellationToken)
    {
        Log.Debug($"Load Bitmap for {displayName}");
        Stopwatch sw = Stopwatch.StartNew();

        MemoryStream memoryStream;

        if (fileStream is MemoryStream _memoryStream)
        {
            memoryStream = _memoryStream;
        }
        else
        {
            memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            fileStream.Dispose();
            memoryStream.Position = 0;
        }

        var decoder = wic.CreateDecoderFromStream(memoryStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

        ColorSpaceInfo colorSpace = GetColorSpaceInfo(decoder);
        cancellationToken.ThrowIfCancellationRequested();

        var frame = decoder.GetFrame(0);
        var directXCompatibleBitmap = ConvertToDirectXCompatibleFormat(frame, out var directXPixelFormat);
        var rotatedBitmap = ApplyOrientationFlag(directXCompatibleBitmap, frame, decoder);
        byte[] pixels = rotatedBitmap.GetPixels();
        cancellationToken.ThrowIfCancellationRequested();

        rotatedBitmap.GetSize(out int width, out int height);

        try
        {
            var canvasBitmap = CanvasBitmap.CreateFromBytes(Device, pixels, width, height, directXPixelFormat);
            cancellationToken.ThrowIfCancellationRequested();

            sw.Stop();
            Log.Debug("Bitmap loaded in " + sw.ElapsedMilliseconds + " ms");

            return new CanvasBitmapImageModel(displayName, canvasBitmap, colorSpace);
        }
        catch (ArgumentException ex) when (ex.HResult == -2147024809)
        // TODO: find faster and better solution, but checking the canvas device (Device.MaximumBitmapSizeInPixels) is very slow (~100ms)
        {
            Log.Debug($"Load {displayName} as CanvasVirtualBitmap");
            var canvasVirtualBitmap = await CanvasVirtualBitmap.LoadAsync(Device, memoryStream.AsRandomAccessStream()).AsTask(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            sw.Stop();
            Log.Debug("Bitmap loaded in " + sw.ElapsedMilliseconds + " ms");

            return new CanvasVirtualBitmapImageModel(displayName, canvasVirtualBitmap, colorSpace);
        }
    }

    private IWICBitmapSource ConvertToDirectXCompatibleFormat(IWICBitmapSource bitmap, out DirectXPixelFormat directXPixelFormat)
    {
        // We are converting to the default format of Win2D (B8G8R8A8UIntNormalized)
        // see https://github.com/microsoft/Win2D/blob/52ef02a7cfbcb476c13d9b87860f429aff15c2b9/winrt/lib/images/CanvasBitmap.cpp#L266
        var targetWicPixelFormat = WICPixelFormat.WICPixelFormat32bppPBGRA;
        directXPixelFormat = DirectXPixelFormat.B8G8R8A8UIntNormalized;

        // alternative conversions would be required to better support HDR images

        var convertedBitmap = wic.CreateFormatConverter();
        convertedBitmap.Initialize(bitmap, targetWicPixelFormat, WICBitmapDitherType.WICBitmapDitherTypeNone, null, 0, WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut);
        return convertedBitmap;
    }

    private IWICBitmapSource ApplyOrientationFlag(IWICBitmapSource bitmap, IWICBitmapFrameDecode frame, IWICBitmapDecoder decoder)
    {
        if (MetadataProperties.Orientation.SupportedFormats.Contains(decoder.GetContainerFormat()))
        {
            var metadataReader = new MetadataReader(frame.GetMetadataQueryReader(), decoder.GetDecoderInfo());
            var orientation = metadataReader.GetProperty(MetadataProperties.Orientation);
            var transformOptions = OrientationToTransformOptions(orientation);

            if (transformOptions != WICBitmapTransformOptions.WICBitmapTransformRotate0)
            {
                var rotatedBitmap = wic.CreateBitmapFlipRotator();
                rotatedBitmap.Initialize(bitmap, transformOptions);
                return rotatedBitmap;
            }
        }
        return bitmap;
    }

    private ColorSpaceInfo GetColorSpaceInfo(IWICBitmapDecoder decoder)
    {
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

        if (exifColorSpace == ExifColorSpace.SRGB || exifColorSpace == ExifColorSpace.Uncalibrated)
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

    private WICBitmapTransformOptions OrientationToTransformOptions(PhotoOrientation orientation)
    {
        return orientation switch
        {
            PhotoOrientation.Rotate90 => WICBitmapTransformOptions.WICBitmapTransformRotate270,
            PhotoOrientation.Rotate180 => WICBitmapTransformOptions.WICBitmapTransformRotate180,
            PhotoOrientation.Rotate270 => WICBitmapTransformOptions.WICBitmapTransformRotate90,
            PhotoOrientation.FlipHorizontal => WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal,
            PhotoOrientation.FlipVertical => WICBitmapTransformOptions.WICBitmapTransformFlipVertical,
            PhotoOrientation.Transpose => WICBitmapTransformOptions.WICBitmapTransformRotate270 | WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal,
            PhotoOrientation.Transverse => WICBitmapTransformOptions.WICBitmapTransformRotate90 | WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal,
            _ => WICBitmapTransformOptions.WICBitmapTransformRotate0
        };
    }

    private IWICBitmapEncoderInfo? TryGetDeocderInfoForFileExtension(string fileExtension)
    {
        var installedEncoders = wic
            .CreateComponentEnumerator(WICComponentType.WICEncoder, WICComponentEnumerateOptions.WICComponentEnumerateDefault)
            .AsEnumerable()
            .OfType<IWICBitmapEncoderInfo>();

        foreach (var encoderInfo in installedEncoders)
        {
            if (encoderInfo.GetFileExtensions().Contains(fileExtension.ToLower()))
            {
                return encoderInfo;
            }
        }
        return null;
    }

}
