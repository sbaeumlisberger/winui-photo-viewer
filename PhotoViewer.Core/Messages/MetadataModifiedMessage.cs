using MetadataAPI;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

public record class MetadataModifiedMessage(IReadOnlyCollection<IBitmapFileInfo> Files, IMetadataProperty MetadataProperty);