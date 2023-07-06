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
        if (deleteLinkedFiles)
        {
            foreach(var linkedFile in media.LinkedStorageFiles)
            {
                await DeleteFileAsync(linkedFile).ConfigureAwait(false);
            }
        }

        await DeleteFileAsync(media.StorageFile).ConfigureAwait(false);
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
