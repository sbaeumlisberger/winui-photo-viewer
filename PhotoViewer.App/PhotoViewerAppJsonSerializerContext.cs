using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PhotoViewer.App;

[JsonSerializable(typeof(JsonNode))]
internal partial class PhotoViewerAppJsonSerializerContext : JsonSerializerContext
{
}