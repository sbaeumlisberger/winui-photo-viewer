using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public sealed class FileSavePickerModel
{
    public string? SuggestedFileName { get; set; }
    public Dictionary<string, IList<string>>? FileTypeChoices { get; set; }
    public IStorageFile? File { get; set; }
}
