using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Utils;
using System.IO.Hashing;
using System.Text;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.Models;

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

    private byte[]? mtpBuffer = null;

    public ulong Id { get; }

    public MediaFileInfoBase(IStorageFile file)
    {
        StorageFile = file;
        Id = GetIdForFilePath(file.Path);
    }

    public static ulong GetIdForFilePath(string filePath)
    {
        return XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(filePath));
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
            return await OpenMTPAsync(fileAccessMode).ConfigureAwait(false);
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
            if (FilePath is not null)
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(FilePath);
                var creationTime = File.GetCreationTimeUtc(FilePath);
                dateModified = lastWriteTime > creationTime ? lastWriteTime : creationTime;
            }
            else
            {
                var basicProperties = await StorageFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
                dateModified = basicProperties.DateModified;
                fileSize = basicProperties.Size;
            }
        }
        return dateModified.Value;
    }

    public async Task<ulong> GetFileSizeAsync()
    {
        if (fileSize is null)
        {
            if (FilePath is not null)
            {
                fileSize = (ulong)new FileInfo(FilePath).Length;
            }
            else
            {
                var basicProperties = await StorageFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
                fileSize = basicProperties.Size;
                dateModified = basicProperties.DateModified;
            }
        }
        return fileSize.Value;
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

    private async Task<Stream> OpenMTPAsync(FileAccessMode fileAccessMode)
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
            using (await mtpLock.AcquireAsync().ConfigureAwait(false))
            {
                if (mtpBuffer == null)
                {
                    using var fileStream = await StorageFile.OpenAsync(fileAccessMode).AsTask().ConfigureAwait(false);

                    if (fileStream.Size > 1024 * 1024 * 100)
                    {
                        // Do not load files larger than 100 MB
                        throw new Exception("File to large!");
                    }

                    mtpBuffer = await fileStream.ReadBytesAsync().ConfigureAwait(false);
                }
            }
        }

        return new MemoryStream(mtpBuffer);
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
        return GetType().Name + "(" + DisplayName + ")";
    }

}
