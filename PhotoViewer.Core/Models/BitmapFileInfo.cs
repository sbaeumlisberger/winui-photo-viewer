using MetadataAPI;
using MetadataAPI.Data;
using WIC;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.Models;

public interface IBitmapFileInfo : IMediaFileInfo
{
    bool IsMetadataSupported { get; }

    bool IsReadMetadataSupported { get; }

    void LinkStorageFile(IStorageFile storageFile);
}

public class BitmapFileInfo : MediaFileInfoBase, IBitmapFileInfo
{
    public static readonly IReadOnlySet<string> BmpFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".bmp", ".dib" };
    public static readonly IReadOnlySet<string> GifFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".gif" };
    public static readonly IReadOnlySet<string> IcoFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ico" };
    public static readonly IReadOnlySet<string> JpegFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpe", ".jpg", ".jfif" };
    public static readonly IReadOnlySet<string> PngFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", };
    public static readonly IReadOnlySet<string> TiffFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".tiff", ".tif" };
    public static readonly IReadOnlySet<string> JpegXrFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jxr", ".wdp", };
    public static readonly IReadOnlySet<string> HeifFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".heic", ".heif" };
    public static readonly IReadOnlySet<string> WebpFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".webp" };

    public static readonly IReadOnlySet<string> CommonFileExtensions =
         BmpFileExtensions
        .Concat(GifFileExtensions)
        .Concat(IcoFileExtensions)
        .Concat(JpegFileExtensions)
        .Concat(PngFileExtensions)
        .Concat(TiffFileExtensions)
        .Concat(JpegXrFileExtensions)
        .Concat(HeifFileExtensions)
        .Concat(WebpFileExtensions)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> RawFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".arw", ".cr2", ".crw", ".erf", ".kdc", ".mrw", ".nef", ".nrw", ".orf", ".pef", ".raf", ".raw", ".rw2", ".rwl", ".sr2", ".srw", ".dng"
    };

    public static readonly IReadOnlySet<string> RawMetadataFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".xmp"
    };

    public bool IsMetadataSupported { get; }

    public bool IsReadMetadataSupported { get; }

    public override IReadOnlyList<IStorageFile> LinkedStorageFiles => linkedStorageFiles;

    private readonly List<IStorageFile> linkedStorageFiles = [];

    private Size sizeInPixels = Size.Empty;

    public BitmapFileInfo(IStorageFile file) : base(file)
    {
        IsMetadataSupported = JpegFileExtensions.Contains(FileExtension) || TiffFileExtensions.Contains(FileExtension);
        IsReadMetadataSupported = IsMetadataSupported || HeifFileExtensions.Contains(FileExtension);
    }

    public void LinkStorageFile(IStorageFile storageFile)
    {
        linkedStorageFiles.Add(storageFile);
    }

    public override async Task<Size> GetSizeInPixelsAsync()
    {
        if (sizeInPixels == Size.Empty)
        {
            sizeInPixels = await Task.Run(async () =>
            {
                using var fileStream = await OpenAsync(FileAccessMode.Read).ConfigureAwait(false);

                var wic = WICImagingFactory.Create();

                var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

                decoder.GetFrame(0).GetSize(out int width, out int height);

                if (MetadataProperties.Orientation.SupportedFormats.Contains(decoder.GetContainerFormat()))
                {
                    var metadataQueryReader = decoder.GetFrame(0).GetMetadataQueryReader();
                    var metadataReader = new MetadataReader(metadataQueryReader, decoder.GetDecoderInfo());

                    var orientation = metadataReader.GetProperty(MetadataProperties.Orientation);

                    if (orientation == PhotoOrientation.Rotate90 || orientation == PhotoOrientation.Rotate270)
                    {
                        return new Size(height, width);
                    }
                }

                return new Size(width, height);

            }).ConfigureAwait(false);
        }
        return sizeInPixels;
    }

    public override async Task<IRandomAccessStream?> GetThumbnailAsync()
    {
        // loading the full image is much faster and always up-to-date
        return await OpenAsRandomAccessStreamAsync(FileAccessMode.Read).ConfigureAwait(false);
    }

    public override void InvalidateCache()
    {
        base.InvalidateCache();
        sizeInPixels = Size.Empty;
    }

}
