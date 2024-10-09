using CommunityToolkit.Mvvm.Input;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public partial class MoveRawFilesToSubfolderDialogModel : ViewModelBase
{
    public bool ShowConfirmation { get; private set; } = true;
    public bool ShowProgress { get; private set; } = false;
    public bool ShowErrorMessage { get; private set; } = false;
    public bool ShowSuccessMessage { get; private set; } = false;

    public Progress? Progress { get; private set; }

    public IReadOnlyList<string> Errors { get; private set; } = new List<string>();


    private readonly IReadOnlyCollection<IMediaFileInfo> mediaFiles;

    private readonly ApplicationSettings settings;

    public MoveRawFilesToSubfolderDialogModel(IReadOnlyCollection<IMediaFileInfo> mediaFiles, ApplicationSettings settings)
    {
        this.mediaFiles = mediaFiles;
        this.settings = settings;
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        var cts = new CancellationTokenSource();
        Progress = new Progress(cts);

        ShowConfirmation = false;
        ShowProgress = true;

        try
        {
            var result = await MoveRawFilesToSubfolderAsync(mediaFiles, Progress, cts.Token);

            ShowProgress = false;
            ShowSuccessMessage = result.IsSuccessfully;
            ShowErrorMessage = result.HasFailures;
            Errors = result.Failures.Select(failure => failure.Element.Name + ": " + failure.Exception.Message).ToList();
        }
        catch (OperationCanceledException)
        {
            // canceld by user
        }
        catch (Exception ex)
        {
            Log.Error("Failed to move RAW files to subfolder", ex);
            ShowProgress = false;
            ShowErrorMessage = true;
            Errors = new List<string> { ex.Message };
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<ParallelResult<IStorageFile>> MoveRawFilesToSubfolderAsync(IReadOnlyCollection<IMediaFileInfo> files, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var filesToMove = new List<IStorageFile>();
        foreach (var mediaFile in files)
        {
            foreach (var storageFile in mediaFile.StorageFiles)
            {
                if ((BitmapFileInfo.RawFileExtensions.Contains(storageFile.FileType.ToLower())
                    || BitmapFileInfo.RawMetadataFileExtensions.Contains(storageFile.FileType.ToLower()))
                    && Path.GetDirectoryName(storageFile.Path) is string directoryPath
                    && !directoryPath.EndsWith("/" + settings.RawFilesFolderName))
                {
                    filesToMove.Add(storageFile);
                }
            }
        }

        if (filesToMove.IsEmpty())
        {
            return ParallelResult<IStorageFile>.Empty;
        }

        var folder = await filesToMove.First().GetParentAsync();
        var rawFilesFolder = await folder.CreateFolderAsync(settings.RawFilesFolderName, CreationCollisionOption.OpenIfExists);
        cancellationToken.ThrowIfCancellationRequested();

        return await filesToMove.Parallel(cancellationToken, progress).TryProcessAsync(async file =>
        {
            await file.MoveAsync(rawFilesFolder);
        });
    }
}
