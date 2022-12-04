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

    IReadOnlyList<IStorageFile> LinkedStorageFiles { get; }

    Task<IRandomAccessStream> OpenAsync(FileAccessMode fileAccessMode);

    Task<DateTimeOffset> GetDateModifiedAsync();

    Task<ulong> GetFileSizeAsync();

    Task<IRandomAccessStream?> GetThumbnailAsync();
}
