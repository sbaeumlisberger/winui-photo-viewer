using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
