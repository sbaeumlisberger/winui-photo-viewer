using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

public record class MediaFileRestoredMessage(IMediaFileInfo File, int Index);
