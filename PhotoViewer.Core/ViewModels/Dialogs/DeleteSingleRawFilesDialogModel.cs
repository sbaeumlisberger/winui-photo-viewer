using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class DeleteSingleRawFilesDialogModel : ViewModelBase
{

    public bool ShowConfirmation { get; private set; } = true;

    public bool ShowProgress { get; private set; } = false;

    public bool ShowErrorMessage { get; private set; } = false;

    public bool ShowSuccessMessage { get; private set; } = false;

    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    public Progress? Progress { get; set; }

    private IReadOnlyCollection<IMediaFileInfo> mediaFiles;

    public DeleteSingleRawFilesDialogModel(IMessenger messenger, IReadOnlyCollection<IMediaFileInfo> mediaFiles) : base(messenger)
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

        try
        {
            var result = await DeleteSingleRawFilesAsync(mediaFiles, Progress, cts.Token);

            ShowProgress = false;
            ShowSuccessMessage = result.IsSuccessfully;
            ShowErrorMessage = result.HasFailures;
            Errors = result.Failures.Select(failure => failure.Element.DisplayName + ": " + failure.Exception.Message).ToList();
        }
        catch (OperationCanceledException)
        {
            // canceld by user
        }
        catch (Exception ex)
        {
            Log.Error("Failed to delete single raw files", ex);
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

    private async Task<ParallelResult<IMediaFileInfo>> DeleteSingleRawFilesAsync(IReadOnlyCollection<IMediaFileInfo> mediaFiles, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var mediaFilesToDelete = mediaFiles
            .Where(mediaFile => BitmapFileInfo.RawFileExtensions.Contains(mediaFile.FileExtension.ToLower())
                && !ExistsFileWithSameName(mediaFiles, mediaFile))
            .ToList();

        var result = await mediaFilesToDelete.Parallel(cancellationToken, progress).TryProcessAsync(async mediaFile =>
        {
            foreach (var storageFile in mediaFile.StorageFiles)
            {
                try
                {
                    await storageFile.DeleteAsync().AsTask().ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    // files does no longer exist
                }
            }
        });

        Messenger.Send(new MediaFilesDeletedMessage(result.ProcessedElements));

        return result;
    }

    private static bool ExistsFileWithSameName(IReadOnlyCollection<IMediaFileInfo> mediaFiles, IMediaFileInfo mediaFile)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(mediaFile.FileName);
        var others = mediaFiles.Except(Enumerable.Repeat(mediaFile, 1));
        return others.Any(other => Path.GetFileNameWithoutExtension(other.FileName) == fileNameWithoutExtension);
    }
}
