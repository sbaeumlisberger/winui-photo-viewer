using PhotoViewerApp.Models;
using System.Collections.Generic;

namespace PhotoViewerApp.Messages;

internal record class MediaItemsDeletedMessage(List<IMediaFileInfo> MediaItems);
