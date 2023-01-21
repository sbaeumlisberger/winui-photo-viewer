using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace PhotoViewerApp.Models;

public interface IMediaFileInfo
{
    string Name { get; }

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
