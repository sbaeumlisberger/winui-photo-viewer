using Windows.Storage;
using Windows.Storage.Pickers;

namespace PhotoViewerApp.ViewModels;

public sealed class FolderPickerModel
{
    public PickerLocationId SuggestedStartLocation { get; set; }
    public StorageFolder? Folder { get; set; }
}
