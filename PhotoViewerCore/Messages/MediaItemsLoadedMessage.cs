using PhotoViewerApp.Models;

namespace PhotoViewerApp.Messages;

public record class MediaItemsLoadedMessage(List<IMediaFileInfo> MediaItems, IMediaFileInfo? StartItem);