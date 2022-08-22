using PhotoViewerApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.Messages;

internal record class MediaItemsLoadedMessage(List<IMediaItem> MediaItems, IMediaItem? StartItem);