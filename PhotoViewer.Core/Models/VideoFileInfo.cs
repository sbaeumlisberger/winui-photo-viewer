using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage;

namespace PhotoViewer.App.Models;

public interface IVideoFileInfo : IMediaFileInfo
{
}

internal class VideoFileInfo : MediaFileInfoBase, IVideoFileInfo
{

    public static readonly IReadOnlySet<string> SupportedFileExtensions = new HashSet<string>()
    {
        ".mp4", ".avi", ".webm", ".mkv", ".ts" // TODO add more
    };

    public VideoFileInfo(IStorageFile file) : base(file)
    {
    }

    public override async Task<Size> GetSizeInPixelsAsync()
    {
        if (StorageFile is StorageFile storageFile)
        {
            var videoProperties = await storageFile.Properties.GetVideoPropertiesAsync();
            return new Size(videoProperties.Width, videoProperties.Height);
        }
        return Size.Empty;
    }

}
