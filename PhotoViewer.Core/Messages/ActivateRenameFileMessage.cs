using PhotoViewer.App.Models;

namespace PhotoViewer.Core.Messages;

internal record class ActivateRenameFileMessage(IMediaFileInfo MediaFile);