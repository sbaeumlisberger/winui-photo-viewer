using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public sealed class FileOpenPickerModel
{
    public IList<string>? FileTypeFilter { get; set; }
    public IStorageFile? File { get; set; }
}
