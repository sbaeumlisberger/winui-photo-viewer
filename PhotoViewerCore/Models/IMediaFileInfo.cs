using Windows.Storage;

namespace PhotoViewerApp.Models;

public interface IMediaFileInfo
{
    string Name { get; }

    IStorageFile File { get; }

    IReadOnlyList<IStorageFile> LinkedFiles { get => Array.Empty<IStorageFile>(); }

    Task<DateTimeOffset> GetDateModifiedAsync();

    Task<ulong> GetFileSizeAsync();
}
