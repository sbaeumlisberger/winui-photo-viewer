using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using Tocronx.SimpleAsync;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace PhotoViewer.App.Models;

public abstract class MediaFileInfoBase : IMediaFileInfo
{
    private static readonly AsyncLock mtpLock = new AsyncLock();

    public string DisplayName => StorageFile.Name + (LinkedStorageFiles.Any() ? "[" + string.Join("|", LinkedStorageFiles.Select(file => file.FileType)) + "]" : string.Empty);

    public IStorageFile StorageFile { get; }

    public string FileName => StorageFile.Name;

    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FileName);

    public string FilePath => StorageFile.Path;

    public string FileExtension => StorageFile.FileType;

    public string ContentType => StorageFile.ContentType;

    public virtual IReadOnlyList<IStorageFile> LinkedStorageFiles => Array.Empty<IStorageFile>();

    public IEnumerable<IStorageFile> StorageFiles => Enumerable.Repeat(StorageFile, 1).Concat(LinkedStorageFiles);

    private DateTimeOffset? dateModified;

    private ulong? fileSize;

    private Windows.Storage.Streams.Buffer? mtpBuffer;

#if DEBUG
    public readonly string DebugId;
#endif

    public MediaFileInfoBase(IStorageFile file)
    {
        StorageFile = file;
#if DEBUG
        DebugId = FileName;
#endif
    }

    public async Task<IRandomAccessStream> OpenAsRandomAccessStreamAsync(FileAccessMode fileAccessMode)
    {
        return (await OpenAsync(fileAccessMode)).AsRandomAccessStream();
    }

    public async Task<Stream> OpenAsync(FileAccessMode fileAccessMode)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            // If the file path is not set, it is probably a file accessed via MTP.            
            return (await OpenMTPAsync(fileAccessMode).ConfigureAwait(false)).AsStream();
        }

        if (fileAccessMode == FileAccessMode.Read)
        {
            return File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        return File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
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

    private async Task LoadBasicPropertiesAsync()
    {
        var basicProperties = await StorageFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
        dateModified = basicProperties.DateModified;
        fileSize = basicProperties.Size;
    }

    public abstract Task<Size> GetSizeInPixelsAsync();

    public virtual async Task<IRandomAccessStream?> GetThumbnailAsync()
    {
        if (StorageFile is StorageFile storageFile)
        {
            return await storageFile.GetThumbnailAsync(ThumbnailMode.SingleItem).AsTask().ConfigureAwait(false);
        }
        return null;
    }

    public virtual void InvalidateCache()
    {
        dateModified = null;
        fileSize = null;
        mtpBuffer = null;
    }

    private async Task<IRandomAccessStream> OpenMTPAsync(FileAccessMode fileAccessMode)
    {
        if (fileAccessMode != FileAccessMode.Read)
        {
            // Files access via MTP are readonly
            throw new Exception("Can not write file via MTP!");
        }

        if (mtpBuffer == null)
        {
            // MTP allows no paralled file operations. Therefore all operations are
            // synchronized. Due to many issues with passing the file streams to
            // native code like Win2D, the stream is copied to an in-memory stream.
            using (await mtpLock.GetLookAsync().ConfigureAwait(false))
            {
                if (mtpBuffer == null)
                {
                    using var fileStream = await StorageFile.OpenAsync(fileAccessMode).AsTask().ConfigureAwait(false);

                    if (fileStream.Size > 1024 * 1024 * 100)
                    {
                        // Do not load files larger than 100 MB
                        throw new Exception("File to large!");
                    }

                    mtpBuffer = new Windows.Storage.Streams.Buffer((uint)fileStream.Size);
                    await fileStream.ReadAsync(mtpBuffer, mtpBuffer.Capacity, InputStreamOptions.None).AsTask().ConfigureAwait(false);
                }
            }
        }

        var memoryStream = new InMemoryRandomAccessStream();
        await memoryStream.WriteAsync(mtpBuffer).AsTask().ConfigureAwait(false);
        await memoryStream.FlushAsync().AsTask().ConfigureAwait(false);
        memoryStream.Seek(0);
        return memoryStream;
    }

    public async Task RenameAsync(string newName)
    {
        foreach (var file in StorageFiles)
        {
            string newFileName = newName + file.FileType;
            Log.Info($"Rename {file.Name} to {newFileName}");
            await file.RenameAsync(newFileName).AsTask().ConfigureAwait(false);
        }
    }

    public override string ToString()
    {
        return DisplayName;
    }

}
