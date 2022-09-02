using PhotoViewerApp.Models;
using System.Collections.Generic;

namespace PhotoViewerApp.Messages;

public record class MediaItemsLoadedMessage(List<IMediaFileInfo> MediaItems, IMediaFileInfo? StartItem);