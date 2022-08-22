using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Models;

public interface IMediaItem
{
    string Name { get; }

    IStorageFile File { get; }

    Task<DateTimeOffset> GetDateModifiedAsync();

    Task<ulong> GetFileSizeAsync();

    Task DeleteAsync();
}
