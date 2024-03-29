﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Linq;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels.Dialogs;

public partial class ImportGpxTrackDialogModel : ViewModelBase
{

    public bool ShowFileSelection { get; private set; } = true;

    public bool ShowProgress { get; private set; } = false;

    public bool ShowErrorMessage { get; private set; } = false;

    public bool ShowSuccessMessage { get; private set; } = false;

    public string SelectedFilePath => SelectedFile?.Path ?? string.Empty;

    private IStorageFile? SelectedFile { get; set; }

    public bool CanImport => SelectedFilePath != string.Empty;

    public Progress? Progress { get; private set; }

    public int UpdatedFilesCount { get; private set; } = 0;

    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private readonly IGpxService gpxService;

    private readonly IReadOnlyList<IMediaFileInfo> mediaFiles;

    internal ImportGpxTrackDialogModel(IMessenger messenger, IMetadataService metadataService, IDialogService dialogService, IGpxService gpxService, IReadOnlyList<IMediaFileInfo> mediaFiles) : base(messenger)
    {
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        this.gpxService = gpxService;
        this.mediaFiles = mediaFiles;
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var fileOpenPickerModel = new FileOpenPickerModel()
        {
            FileTypeFilter = new[] { ".gpx" }
        };

        await dialogService.ShowDialogAsync(fileOpenPickerModel);

        SelectedFile = fileOpenPickerModel.File;
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        var cts = new CancellationTokenSource();
        Progress = new Progress(cts);

        ShowFileSelection = false;
        ShowProgress = true;

        try
        {
            var result = await ImportGpxFileAsync(SelectedFile!, mediaFiles, Progress, cts.Token);

            ShowProgress = false;
            ShowSuccessMessage = result.IsSuccessfully;
            UpdatedFilesCount = result.ProcessedElements.Count;
            ShowErrorMessage = result.HasFailures;
            Errors = result.Failures.Select(failure => failure.Element.FileName + ": " + failure.Exception.Message).ToList();
        }
        catch (OperationCanceledException)
        {
            // canceld by user
        }
        catch (Exception ex)
        {
            Log.Error("Failed to import GPX file", ex);
            ShowProgress = false;
            ShowErrorMessage = true;
            Errors = new[] { ex.Message };
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<ParallelResult<IBitmapFileInfo>> ImportGpxFileAsync(IStorageFile gpxFile, IReadOnlyList<IMediaFileInfo> mediaFiles, Progress progress, CancellationToken cancellationToken)
    {
        var gpxTrack = await gpxService.ReadTrackFromGpxFileAsync(gpxFile);

        var mediaFilesToProcess = mediaFiles.OfType<IBitmapFileInfo>().Where(bitmap => bitmap.IsMetadataSupported).ToList();

        var result = await mediaFilesToProcess.Parallel(cancellationToken, progress).TryProcessAsync(async mediaFile =>
        {
            await gpxService.TryApplyGpxTrackToFile(gpxTrack, mediaFile);
        });

        Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.GeoTag));

        return result;
    }

}
