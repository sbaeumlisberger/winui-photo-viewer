using PhotoViewer.App.Models;

namespace PhotoViewer.App.Messages;

public record class MediaFilesDeletedMessage(IReadOnlyCollection<IMediaFileInfo> Files);
