using PhotoViewerApp.Models;

namespace PhotoViewerApp.Messages;

public record class MediaFilesLoadedMessage(List<IMediaFileInfo> Files, IMediaFileInfo? StartFile);