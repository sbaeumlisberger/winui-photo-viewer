using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PhotoViewer.App;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonNode))]
internal partial class PhotoViewerAppJsonSerializerContext : JsonSerializerContext
{
}