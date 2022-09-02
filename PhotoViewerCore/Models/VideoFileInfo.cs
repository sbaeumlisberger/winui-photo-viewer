using System.Collections.Generic;
using Windows.Storage;

namespace PhotoViewerApp.Models;

internal class VideoFileInfo : MediaFileInfoBase
{

    public static readonly IReadOnlySet<string> SupportedFileExtensions = new HashSet<string>()
    {
        ".mp4", ".avi", ".webm", ".mkv", ".ts" // TODO add more
    };

    public VideoFileInfo(IStorageFile file) : base(file)
    {
    }

}
