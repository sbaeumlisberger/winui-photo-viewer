using PhotoViewerApp.Models;
using Windows.Storage;

namespace PhotoViewerApp.Services;

public interface IDeleteMediaService
{
    Task DeleteMediaAsync(IMediaFileInfo media, bool deleteLinkedFiles);
}

internal class DeleteMediaService : IDeleteMediaService
{
    public async Task DeleteMediaAsync(IMediaFileInfo media, bool deleteLinkedFiles)
    {
        await DeleteFileAsync(media.StorageFile).ConfigureAwait(false);

        if (deleteLinkedFiles)
        {
            await Task.WhenAll(media.LinkedStorageFiles.Select(DeleteFileAsync)).ConfigureAwait(false);
        }
    }

    private async Task DeleteFileAsync(IStorageFile file)
    {
        try
        {
            await file.DeleteAsync().AsTask().ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            // files does no longer exist
        }
    }
}
