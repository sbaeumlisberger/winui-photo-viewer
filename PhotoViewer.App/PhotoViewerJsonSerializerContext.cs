using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PhotoViewer.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonNode))]
internal partial class PhotoViewerJsonSerializerContext : JsonSerializerContext
{
}