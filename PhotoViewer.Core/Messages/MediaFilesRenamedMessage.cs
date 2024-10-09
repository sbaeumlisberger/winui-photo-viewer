using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

internal record class MediaFilesRenamedMessage(List<IMediaFileInfo> MediaFiles) { }