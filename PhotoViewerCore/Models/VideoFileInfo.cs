using Windows.Storage;

namespace PhotoViewerApp.Models;

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

}
