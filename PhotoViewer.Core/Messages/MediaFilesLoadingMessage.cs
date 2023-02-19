using PhotoViewer.App.Models;
using PhotoViewer.Core.Models;

namespace PhotoViewer.App.Messages;

public record class MediaFilesLoadingMessage(LoadMediaFilesTask LoadMediaFilesTask);