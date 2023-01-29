using MetadataAPI;
using PhotoViewer.App.Models;

namespace PhotoViewerCore.Messages;

public record class MetadataModifiedMessage(IReadOnlyCollection<IBitmapFileInfo> Files, IMetadataProperty MetadataProperty);