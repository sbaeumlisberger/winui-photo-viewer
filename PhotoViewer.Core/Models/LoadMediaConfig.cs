namespace PhotoViewer.Core.Models;

public record class LoadMediaConfig
(
    bool LinkRAWs,
    string? RAWsFolderName,
    bool IncludeVideos
);
