using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

internal record class ActivateRenameFileMessage(IMediaFileInfo MediaFile);