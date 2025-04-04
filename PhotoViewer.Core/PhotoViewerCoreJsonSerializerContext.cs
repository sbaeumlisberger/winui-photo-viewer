﻿using PhotoViewer.Core.Services;
using System.Text.Json.Serialization;

namespace PhotoViewer.Core;

[JsonSerializable(typeof(SuggestionsJsonObject))]
[JsonSerializable(typeof(List<string>))]
internal partial class PhotoViewerCoreJsonSerializerContext : JsonSerializerContext
{
}