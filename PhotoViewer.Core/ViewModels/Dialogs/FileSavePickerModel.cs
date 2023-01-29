using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public sealed class FileSavePickerModel
{
    public string? SuggestedFileName { get; set; }
    public IDictionary<string, IList<string>>? FileTypeChoices { get; set; }
    public IStorageFile? File { get; set; }
}
