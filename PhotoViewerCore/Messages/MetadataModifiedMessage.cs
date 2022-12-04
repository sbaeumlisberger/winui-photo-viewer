using MetadataAPI;
using PhotoViewerApp.Models;

namespace PhotoViewerCore.Messages;

public record class MetadataModifiedMessage(IReadOnlyCollection<IBitmapFileInfo> Files, IMetadataProperty MetadataProperty);