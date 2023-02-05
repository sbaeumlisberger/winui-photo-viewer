using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public class LaunchFileDialogModel
{
    public IStorageFile File { get; }

    public LaunchFileDialogModel(IStorageFile file)
    {
        File = file;
    }
}
