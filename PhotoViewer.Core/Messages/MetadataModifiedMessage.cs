using MetadataAPI;
using PhotoViewer.App.Models;

namespace PhotoViewer.Core.Messages;

public record class MetadataModifiedMessage(IReadOnlyCollection<IBitmapFileInfo> Files, IMetadataProperty MetadataProperty);