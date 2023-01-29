using WIC;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.App.Models;

public interface IBitmapFileInfo : IMediaFileInfo
{
    bool IsMetadataSupported { get; }

    void LinkStorageFile(IStorageFile storageFile);
}

public class BitmapFileInfo : MediaFileInfoBase, IBitmapFileInfo
{
    public static readonly IReadOnlyList<string> BmpFileExtensions = new[] { ".bmp", ".dib" };
    public static readonly IReadOnlyList<string> GifFileExtensions = new[] { ".gif" };
    public static readonly IReadOnlyList<string> IcoFileExtensions = new[] { ".ico" };
    public static readonly IReadOnlyList<string> JpegFileExtensions = new[] { ".jpeg", ".jpe", ".jpg", ".jfif", };
    public static readonly IReadOnlyList<string> PngFileExtensions = new[] { ".png", };
    public static readonly IReadOnlyList<string> TiffFileExtensions = new[] { ".tiff", ".tif" };
    public static readonly IReadOnlyList<string> JpegXrFileExtensions = new[] { ".jxr", ".wdp", };
    public static readonly IReadOnlyList<string> HeifFileExtensions = new[] { ".heic", ".heif" };
    public static readonly IReadOnlyList<string> WebpFileExtensions = new[] { ".webp" };

    public static readonly IReadOnlyCollection<string> CommonFileExtensions =
         BmpFileExtensions
        .Concat(GifFileExtensions)
        .Concat(IcoFileExtensions)
        .Concat(JpegFileExtensions)
        .Concat(PngFileExtensions)
        .Concat(TiffFileExtensions)
        .Concat(JpegXrFileExtensions)
        .Concat(HeifFileExtensions)
        .Concat(WebpFileExtensions)
        .ToHashSet();

    public static readonly IReadOnlySet<string> RawFileExtensions = new HashSet<string>()
    {
        ".arw", ".cr2", ".crw", ".erf", ".kdc", ".mrw", ".nef", ".nrw", ".orf",
         ".pef", ".raf", ".raw", ".rw2", ".rwl", ".sr2", ".srw", ".dng",  ".xmp"
    };

    public static readonly IReadOnlySet<string> SupportedFileExtensions = CommonFileExtensions.Concat(RawFileExtensions).ToHashSet();

    public bool IsMetadataSupported { get; }

    public override IReadOnlyList<IStorageFile> LinkedStorageFiles => linkedStorageFiles;

    private List<IStorageFile> linkedStorageFiles = new List<IStorageFile>();

    private Size sizeInPixels = Size.Empty;

    public BitmapFileInfo(IStorageFile file) : base(file)
    {
        IsMetadataSupported = JpegFileExtensions.Contains(StorageFile.FileType.ToLower()) || TiffFileExtensions.Contains(StorageFile.FileType.ToLower());
    }

    public void LinkStorageFile(IStorageFile storageFile)
    {
        linkedStorageFiles.Add(storageFile);
    }

    public override async Task<Size> GetSizeInPixelsAsync()
    {
        if (sizeInPixels == Size.Empty)
        {
            using var fileStream = await OpenAsync(FileAccessMode.Read).ConfigureAwait(false);

            var wic = new WICImagingFactory();

            var decoder = wic.CreateDecoderFromStream(fileStream.AsStream(), WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

            decoder.GetFrame(0).GetSize(out int width, out int height);

            sizeInPixels = new Size(width, height);
        }
        return sizeInPixels;
    }

    public override async Task<IRandomAccessStream?> GetThumbnailAsync()
    {
        // loading the image directly is much faster and always up-to-date
        return await OpenAsync(FileAccessMode.Read).ConfigureAwait(false);
    }

    public override void InvalidateCache()
    {
        base.InvalidateCache();
        sizeInPixels = Size.Empty;
    }

}
