using PhotoViewer.App.Models;
using Windows.Storage;

namespace PhotoViewer.App.Services;

public interface IDeleteMediaFilesService
{
    Task DeleteMediaFileAsync(IMediaFileInfo media, bool deleteLinkedFiles);
}

internal class DeleteMediaFilesService : IDeleteMediaFilesService
{
    public async Task DeleteMediaFileAsync(IMediaFileInfo media, bool deleteLinkedFiles)
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
