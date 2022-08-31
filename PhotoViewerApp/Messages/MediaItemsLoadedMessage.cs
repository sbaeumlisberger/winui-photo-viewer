using PhotoViewerApp.Models;
using System.Collections.Generic;

namespace PhotoViewerApp.Messages;

internal record class MediaItemsLoadedMessage(List<IMediaFileInfo> MediaItems, IMediaFileInfo? StartItem);