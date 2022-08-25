using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Models;

public class BitmapGraphicItem : IMediaItem
{
    public static readonly IReadOnlySet<string> CommonFileExtensions = new HashSet<string>()
    {
        ".bmp", ".dib" ,".gif" ,".ico" , ".jpeg", ".jpe", ".jpg", ".jfif",
        ".png", ".tiff", ".tif", ".jxr", ".wdp", ".heic", ".heif" , ".webp"
    };

    public static readonly IReadOnlySet<string> RawFileExtensions = new HashSet<string>()
    {
        ".arw", ".cr2", ".crw", ".erf", ".kdc", ".mrw", ".nef", ".nrw", ".orf",
         ".pef", ".raf", ".raw", ".rw2", ".rwl", ".sr2", ".srw", ".dng",  ".xmp"
    };

    public static readonly IReadOnlySet<string> SupportedFileExtensions = CommonFileExtensions.Concat(RawFileExtensions).ToHashSet();

    public string Name => File.Name;

    public IStorageFile File { get; }

    public bool IsMetadataSupported { get; } = false;

    private DateTimeOffset? dateModified;

    private ulong? fileSize;

    public BitmapGraphicItem(IStorageFile storageFile)
    {
        File = storageFile;
    }

    public async Task<DateTimeOffset> GetDateModifiedAsync()
    {
        if (dateModified is null)
        {
            await LoadBasicPropertiesAsync().ConfigureAwait(false);
        }
        return (DateTimeOffset)dateModified!;

    }

    public async Task<ulong> GetFileSizeAsync()
    {
        if (fileSize is null)
        {
            await LoadBasicPropertiesAsync().ConfigureAwait(false);
        }
        return (ulong)fileSize!;
    }

    public async Task DeleteAsync()
    {
        await File.DeleteAsync().AsTask().ConfigureAwait(false);
    }

    private async Task LoadBasicPropertiesAsync()
    {
        var basicProperties = await File.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
        dateModified = basicProperties.DateModified;
        fileSize = basicProperties.Size;
    }

}
