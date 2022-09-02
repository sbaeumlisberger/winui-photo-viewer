using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Models;

public class MediaFileInfoBase : IMediaFileInfo
{
    public virtual string Name => File.Name;

    public IStorageFile File { get; }

    private DateTimeOffset? dateModified;

    private ulong? fileSize;

    public MediaFileInfoBase(IStorageFile file)
    {
        File = file;
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
        var basicProperties = await File.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
        dateModified = basicProperties.DateModified;
        fileSize = basicProperties.Size;
    }
}
