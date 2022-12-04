using PhotoViewerApp.Utils.Logging;
using System;
using System.IO;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace PhotoViewerApp.Models;

public class MediaFileInfoBase : IMediaFileInfo
{
    public string Name => StorageFile.Name + (LinkedStorageFiles.Any() ? "[" + string.Join("|", LinkedStorageFiles.Select(file => file.FileType)) + "]" : string.Empty);

    public IStorageFile StorageFile { get; }

    public string FileName => StorageFile.Name;

    public string FilePath => StorageFile.Path;

    public string FileExtension => StorageFile.FileType.ToLower();

    public virtual IReadOnlyList<IStorageFile> LinkedStorageFiles => Array.Empty<IStorageFile>();

    private DateTimeOffset? dateModified;

    private ulong? fileSize;

    private Windows.Storage.Streams.Buffer? buffer;

    private static SemaphoreSlim? semaphore = new SemaphoreSlim(1, 1);

    public MediaFileInfoBase(IStorageFile file)
    {
        StorageFile = file;
    }

    public async Task<IRandomAccessStream> OpenAsync(FileAccessMode fileAccessMode)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            // If the file path is not set, it is probably a file accessed via MTP.            
            return await OpenMTPAsync(fileAccessMode).ConfigureAwait(false);
        }
        return await StorageFile.OpenAsync(fileAccessMode).AsTask().ConfigureAwait(false);
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

    public virtual async Task<IRandomAccessStream?> GetThumbnailAsync()
    {
        if (StorageFile is StorageFile storageFile) 
        {
            await storageFile.GetThumbnailAsync(ThumbnailMode.SingleItem).AsTask().ConfigureAwait(false);
        }
        return null;
    }

    private async Task<IRandomAccessStream> OpenMTPAsync(FileAccessMode fileAccessMode) 
    {
        if (fileAccessMode != FileAccessMode.Read)
        {
            // Files access via MTP are readonly
            throw new Exception("Can not write file!");
        }

        if (buffer == null)
        {
            // MTP allows no paralled file operations. Therefore all operations are
            // synchronized. Due to many issues with passing the file streams to
            // native code like Win2D, the stream is copied to an in-memory stream.
            await semaphore!.WaitAsync().ConfigureAwait(false);
            try
            {
                if (buffer == null)
                {
                    using var fileStream = await StorageFile.OpenAsync(fileAccessMode).AsTask().ConfigureAwait(false);

                    if (fileStream.Size > 1024 * 1024 * 100)
                    {
                        // Do not load files larger than 100 MB
                        throw new Exception("File to large!");
                    }

                    buffer = new Windows.Storage.Streams.Buffer((uint)fileStream.Size);
                    await fileStream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None).AsTask().ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore!.Release();
            }
        }

        var memoryStream = new InMemoryRandomAccessStream();
        await memoryStream.WriteAsync(buffer).AsTask().ConfigureAwait(false);
        await memoryStream.FlushAsync().AsTask().ConfigureAwait(false);
        memoryStream.Seek(0);
        return memoryStream;
    }
}
