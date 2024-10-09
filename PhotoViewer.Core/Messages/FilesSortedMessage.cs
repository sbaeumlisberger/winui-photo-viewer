using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;

namespace PhotoViewer.Core.Messages;

internal record class FilesSortedMessage(List<IMediaFileInfo> SortedFiles, SortBy SortBy, bool Descending);