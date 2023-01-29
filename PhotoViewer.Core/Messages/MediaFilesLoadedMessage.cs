using PhotoViewer.App.Models;

namespace PhotoViewer.App.Messages;

public record class MediaFilesLoadedMessage(List<IMediaFileInfo> Files, IMediaFileInfo? StartFile);