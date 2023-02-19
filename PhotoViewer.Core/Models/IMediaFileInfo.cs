using System.Numerics;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace PhotoViewer.App.Models;

public interface IMediaFileInfo
{
    string DisplayName { get; }

    IStorageFile StorageFile { get; }

    string FileName { get; }

    string FilePath { get; }

    string FileExtension { get; }

    string ContentType { get; }

    IReadOnlyList<IStorageFile> LinkedStorageFiles { get; }

    IEnumerable<IStorageFile> StorageFiles => new[] { StorageFile }.Concat(LinkedStorageFiles);

    Task<IRandomAccessStream> OpenAsync(FileAccessMode fileAccessMode);

    Task<DateTimeOffset> GetDateModifiedAsync();

    Task<ulong> GetFileSizeAsync();

    Task<Size> GetSizeInPixelsAsync();

    Task<IRandomAccessStream?> GetThumbnailAsync();

    void InvalidateCache();
}
