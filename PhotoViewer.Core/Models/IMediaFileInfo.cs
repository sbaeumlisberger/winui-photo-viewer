using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using Tocronx.SimpleAsync;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.App.Models;

public interface IMediaFileInfo
{
    string DisplayName { get; }

    IStorageFile StorageFile { get; }

    string FileName { get; }

    string FileNameWithoutExtension { get; }

    string FilePath { get; }

    string FileExtension { get; }

    string ContentType { get; }

    IReadOnlyList<IStorageFile> LinkedStorageFiles { get; }

    IEnumerable<IStorageFile> StorageFiles { get; }

    Task<AsyncLockFIFO.AcquiredLock> AcquireExclusiveAccessAsync();

    Task<IRandomAccessStream> OpenAsRandomAccessStreamAsync(FileAccessMode fileAccessMode);
   
    Task<Stream> OpenAsync(FileAccessMode fileAccessMode);

    Task<DateTimeOffset> GetDateModifiedAsync();

    Task<ulong> GetFileSizeAsync();

    Task<Size> GetSizeInPixelsAsync();

    Task<IRandomAccessStream?> GetThumbnailAsync();

    Task RenameAsync(string newName);

    void InvalidateCache();
}
