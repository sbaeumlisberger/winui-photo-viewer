using PhotoViewer.Core.Services;

namespace PhotoViewer.Core.Models;

public class LoadMediaFilesTask
{
    public static LoadMediaFilesTask Empty => new LoadMediaFilesTask(null, Task.FromResult(new LoadMediaFilesResult(new List<IMediaFileInfo>(), null)));

    public IMediaFileInfo? PreviewMediaFile { get; }

    private Task<LoadMediaFilesResult> resultTask;

    public LoadMediaFilesTask(IMediaFileInfo? startMediaFile, Task<LoadMediaFilesResult> resultTask)
    {
        PreviewMediaFile = startMediaFile;
        this.resultTask = resultTask;
    }

    public Task<LoadMediaFilesResult> WaitForResultAsync()
    {
        return resultTask;
    }

}
