using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.Models;

public interface IMediaFileInfo
{
    string DisplayName { get; }

    IStorageFile StorageFile { get; }

    string FileName { get; }

    string FileNameWithoutExtension { get; }

    string FilePath { get; }

    string FileExtension { get; }

    string ContentType { get; }

    ulong Id { get; }

    IReadOnlyList<IStorageFile> LinkedStorageFiles { get; }

    IEnumerable<IStorageFile> StorageFiles { get; }

    Task<IRandomAccessStream> OpenAsRandomAccessStreamAsync(FileAccessMode fileAccessMode);

    Task<Stream> OpenAsync(FileAccessMode fileAccessMode);

    Task<DateTimeOffset> GetDateModifiedAsync();

    Task<ulong> GetFileSizeAsync();

    Task<Size> GetSizeInPixelsAsync();

    Task<IRandomAccessStream?> GetThumbnailAsync();

    Task RenameAsync(string newName);

    void InvalidateCache();
}
