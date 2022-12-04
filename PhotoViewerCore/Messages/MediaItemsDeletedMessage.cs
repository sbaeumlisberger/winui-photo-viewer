using PhotoViewerApp.Models;

namespace PhotoViewerApp.Messages;

public record class MediaItemsDeletedMessage(IReadOnlyCollection<IMediaFileInfo> MediaItems);
