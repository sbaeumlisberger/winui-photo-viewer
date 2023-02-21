using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels.Dialogs;

public partial class DeleteSingleRawFilesDialogModel : ViewModelBase
{

    public bool ShowConfirmation { get; private set; } = true;

    public bool ShowProgress { get; private set; } = false;

    public bool ShowErrorMessage { get; private set; } = false;

    public bool ShowSuccessMessage { get; private set; } = false;

    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    public Progress? Progress { get; set; }

    private IReadOnlyCollection<IMediaFileInfo> mediaFiles;

    public DeleteSingleRawFilesDialogModel(IReadOnlyCollection<IMediaFileInfo> mediaFiles)
    {
        this.mediaFiles = mediaFiles;
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        var cts = new CancellationTokenSource();
        Progress = new Progress(cts);

        ShowConfirmation = false;
        ShowProgress = true;

        var result = await DeleteSingleRawFilesAsync(mediaFiles, Progress, cts.Token);
        
        ShowSuccessMessage = result.IsSuccessful;
        ShowErrorMessage = result.HasFailures;
        Errors = result.Failures.Select(failure => failure.Element.Name + ": " + failure.Exception.Message).ToList();

        ShowProgress = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<ProcessingResult<IStorageFile>> DeleteSingleRawFilesAsync(IReadOnlyCollection<IMediaFileInfo> mediaFiles, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var fileToDelete = mediaFiles
            .Where(mediaFile => BitmapFileInfo.RawFileExtensions.Contains(mediaFile.FileExtension.ToLower())
                                && !ExistsFileWithSameName(mediaFiles, mediaFile))
            .Select(mediaFile => mediaFile.StorageFile)
            .ToList();

        return await ParallelProcessingUtil.ProcessParallelAsync(fileToDelete, async file =>
        {
            await file.DeleteAsync();

        }, progress, cancellationToken);
    }

    private static bool ExistsFileWithSameName(IReadOnlyCollection<IMediaFileInfo> mediaFiles, IMediaFileInfo mediaFile) 
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(mediaFile.FileName);
        var others = mediaFiles.Except(Enumerable.Repeat(mediaFile, 1));
        return others.Any(other => Path.GetFileNameWithoutExtension(other.FileName) == fileNameWithoutExtension);
    }
}
