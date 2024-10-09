using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public sealed class FileOpenPickerModel
{
    public IList<string>? FileTypeFilter { get; set; }
    public IStorageFile? File { get; set; }
}

public sealed class FileOpenPickerModel2
{
    public IList<string>? FileTypeFilter { get; set; }

    public string? InitialFolder { get; set; }

    public string? FilePath { get; set; }
}