using PhotoViewerApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Services;

public interface IDeleteMediaService
{
    Task DeleteMediaAsync(IEnumerable<IMediaFileInfo> mediaList, bool deleteLinkedFiles);

    Task DeleteMediaAsync(IMediaFileInfo media, bool deleteLinkedFiles);
}

internal class DeleteMediaService : IDeleteMediaService
{
    public async Task DeleteMediaAsync(IEnumerable<IMediaFileInfo> mediaList, bool deleteLinkedFiles)
    {
        foreach (var chunk in mediaList.Chunk(4))
        {
            await Task.WhenAll(chunk.Select(media => DeleteMediaAsync(media, deleteLinkedFiles))).ConfigureAwait(false);
        }
    }

    public async Task DeleteMediaAsync(IMediaFileInfo media, bool deleteLinkedFiles)
    {
        await DeleteFileAsync(media.File).ConfigureAwait(false);

        if (deleteLinkedFiles)
        {
            await Task.WhenAll(media.LinkedFiles.Select(DeleteFileAsync)).ConfigureAwait(false);
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
