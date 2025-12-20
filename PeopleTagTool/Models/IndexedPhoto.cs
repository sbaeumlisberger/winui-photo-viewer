using LiteDB;
using System;
using System.Collections.Generic;

namespace PeopleTagTool.Models;

internal class IndexedPhoto
{
    public ObjectId? ID { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public DateTime DateTaken { get; set; }

    public string[] Keywords { get; set; } = [];

    public string[] PeopleTags { get; set; } = [];

    public List<DetectedFaceModel> DetectedFaces { get; set; } = [];
}
