using Windows.Storage;

namespace PhotoViewerCore.ViewModels;

public class LaunchFileDialogModel
{
    public IStorageFile File { get; }

    public LaunchFileDialogModel(IStorageFile file)
    {
        File = file;
    }
}
