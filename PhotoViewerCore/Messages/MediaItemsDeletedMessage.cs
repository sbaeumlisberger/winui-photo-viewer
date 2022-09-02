using PhotoViewerApp.Models;
using System.Collections.Generic;

namespace PhotoViewerApp.Messages;

public record class MediaItemsDeletedMessage(List<IMediaFileInfo> MediaItems);
