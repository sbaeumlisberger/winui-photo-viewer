using PhotoViewerApp.Models;

namespace PhotoViewerApp.Messages;

public record class MediaFilesDeletedMessage(IReadOnlyCollection<IMediaFileInfo> Files);
