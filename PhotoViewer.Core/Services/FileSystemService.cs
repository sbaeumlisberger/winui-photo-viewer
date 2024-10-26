using Windows.Storage;
using Windows.Storage.Search;
using WindowsRecylceBin;

namespace PhotoViewer.Core.Services;

public interface IFileSystemService
{
    Task<List<IStorageFile>> ListFilesAsync(IStorageFolder storageFolder);

    Task<List<IStorageFile>> ListFilesAsync(IStorageQueryResultBase storageQuery);

    Task<IStorageFolder?> TryGetFolderAsync(string path);

    Task<IStorageFile?> TryGetFileAsync(string path);

    bool Exists(string path);

    void Restore(IStorageFile file);
}

public class FileSystemService : IFileSystemService
{
    public async Task<List<IStorageFile>> ListFilesAsync(IStorageFolder storageFolder)
    {
        return (await storageFolder.GetFilesAsync().AsTask().ConfigureAwait(false)).Cast<IStorageFile>().ToList();
    }

    public async Task<List<IStorageFile>> ListFilesAsync(IStorageQueryResultBase storageQuery)
    {
        return (await ((StorageFileQueryResult)storageQuery).GetFilesAsync().AsTask().ConfigureAwait(false)).Cast<IStorageFile>().ToList();
    }

    public async Task<IStorageFolder?> TryGetFolderAsync(string path)
    {
        if (Directory.Exists(path))
        {
            return await StorageFolder.GetFolderFromPathAsync(path).AsTask().ConfigureAwait(false);
        }
        return null;
    }

    public async Task<IStorageFile?> TryGetFileAsync(string path)
    {
        if (File.Exists(path))
        {
            return await StorageFile.GetFileFromPathAsync(path).AsTask().ConfigureAwait(false);
        }
        return null;
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public void Restore(IStorageFile file)
    {
        RecycleBin.ForCurrentUser().Restore(file.Path);
    }
}