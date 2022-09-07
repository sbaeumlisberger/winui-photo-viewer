using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public sealed class FileOpenPickerModel
{
    public IList<string>? FileTypeFilter { get; set; }
    public IStorageFile? File { get; set; }
}
