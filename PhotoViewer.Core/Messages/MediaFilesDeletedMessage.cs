using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

public record class MediaFilesDeletedMessage(IReadOnlyCollection<IMediaFileInfo> Files);
