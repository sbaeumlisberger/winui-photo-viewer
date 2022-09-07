using PhotoViewerApp.Models;

namespace PhotoViewerApp.Messages;

public record class MediaItemsDeletedMessage(List<IMediaFileInfo> MediaItems);
