﻿using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels.Dialogs;

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

            ShowSuccessMessage = result.IsSuccessful;
            ShowErrorMessage = result.HasFailures;
            Errors = result.Failures.Select(failure => failure.Element.Name + ": " + failure.Exception.Message).ToList();

            ShowProgress = false;
        }
        catch (OperationCanceledException)
        {
            Log.Info("Moving RAW files to subfolder was canceled");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<ProcessingResult<IStorageFile>> MoveRawFilesToSubfolderAsync(IReadOnlyCollection<IMediaFileInfo> files, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var filesToMove = new List<IStorageFile>();
        foreach (var mediaFile in files)
        {
            foreach (var storageFile in mediaFile.StorageFiles)
            {
                if (BitmapFileInfo.RawFileExtensions.Contains(storageFile.FileType.ToLower())
                    && Path.GetDirectoryName(storageFile.Path) is string directoryPath
                    && !directoryPath.EndsWith("/" + settings.RawFilesFolderName))
                {
                    filesToMove.Add(storageFile);
                }
            }
        }

        var folder = await filesToMove.First().GetParentAsync();
        var rawFilesFolder = await folder.CreateFolderAsync(settings.RawFilesFolderName, CreationCollisionOption.OpenIfExists);
        cancellationToken.ThrowIfCancellationRequested();

        return await ParallelProcessingUtil.ProcessParallelAsync(filesToMove, async file =>
        {
            await file.MoveAsync(rawFilesFolder);

        }, progress, cancellationToken, throwOnCancellation: true);
    }
}