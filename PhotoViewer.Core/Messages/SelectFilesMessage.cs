using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

internal record class SelectFilesMessage(IReadOnlyList<IMediaFileInfo> FilesToSelect);
