using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Storage;

namespace PhotoViewerApp.Models;

public class BitmapFileInfo : MediaFileInfoBase
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

    public override string Name => File.Name + (LinkedFiles.Any() ? "[" + string.Join("|", LinkedFiles.Select(file => file.FileType)) + "]" : string.Empty);

    public IList<IStorageFile> LinkedFiles { get; } = new List<IStorageFile>();
    
    public BitmapFileInfo(IStorageFile file) : base(file)
    {
        IsMetadataSupported = JpegFileExtensions.Contains(File.FileType.ToLower()) || TiffFileExtensions.Contains(File.FileType.ToLower());
    }

}
