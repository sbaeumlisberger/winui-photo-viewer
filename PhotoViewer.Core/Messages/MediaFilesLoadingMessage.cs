using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

public record class MediaFilesLoadingMessage(LoadMediaFilesTask LoadMediaFilesTask);