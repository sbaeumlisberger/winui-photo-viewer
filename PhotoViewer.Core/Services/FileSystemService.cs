using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace PhotoViewer.Core.Services;

public interface IFileSystemService
{
    Task<List<IStorageFile>> ListFilesAsync(IStorageFolder storageFolder);

    Task<List<IStorageFile>> ListFilesAsync(IStorageQueryResultBase storageQuery);

    Task<IStorageFolder?> TryGetFolderAsync(string filePath);
}

internal class FileSystemService : IFileSystemService
{
    public async Task<List<IStorageFile>> ListFilesAsync(IStorageFolder storageFolder)
    {
        return (await storageFolder.GetFilesAsync().AsTask().ConfigureAwait(false)).Cast<IStorageFile>().ToList();
    }

    public async Task<List<IStorageFile>> ListFilesAsync(IStorageQueryResultBase storageQuery)
    {
        return (await ((StorageFileQueryResult)storageQuery).GetFilesAsync().AsTask().ConfigureAwait(false)).Cast<IStorageFile>().ToList();
    }

    public async Task<IStorageFolder?> TryGetFolderAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            return await StorageFolder.GetFolderFromPathAsync(filePath).AsTask().ConfigureAwait(false);
        }
        return null;
    }
}
