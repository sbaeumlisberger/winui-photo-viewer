using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Concurrent;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public partial class ImportGpxTrackDialogModel : ViewModelBase
{
    public string Title { get; }

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

    private readonly IDialogService dialogService;

    private readonly IGpxService gpxService;

    private readonly IReadOnlyList<IMediaFileInfo> mediaFiles;

    internal ImportGpxTrackDialogModel(IMessenger messenger, IDialogService dialogService, IGpxService gpxService, IReadOnlyList<IMediaFileInfo> mediaFiles, bool allFiles = false) : base(messenger)
    {
        this.dialogService = dialogService;
        this.gpxService = gpxService;
        this.mediaFiles = mediaFiles;

        Title = allFiles ? Strings.ImportGpxTrackDialog_TitleAll : Strings.ImportGpxTrackDialog_Title;
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
            var (result, updatedFilesCount) = await ImportGpxFileAsync(SelectedFile!, mediaFiles, Progress, cts.Token);

            ShowProgress = false;
            ShowSuccessMessage = result.IsSuccessfully;
            UpdatedFilesCount = updatedFilesCount;
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
            Errors = [string.Format(Strings.ImportGpxTrackDialog_GpxFileParseErrorMessage, ex.Message)];
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<(ParallelResult<IBitmapFileInfo> ParallelResult, int UpdatedFilesCount)> ImportGpxFileAsync(IStorageFile gpxFile, IReadOnlyList<IMediaFileInfo> mediaFiles, Progress progress, CancellationToken cancellationToken)
    {
        var gpxTrack = await gpxService.ReadTrackFromGpxFileAsync(gpxFile);

        var mediaFilesToProcess = mediaFiles.OfType<IBitmapFileInfo>().Where(bitmap => bitmap.IsMetadataSupported).ToList();

        var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

        var result = await mediaFilesToProcess.Parallel(cancellationToken, progress).TryProcessAsync(async mediaFile =>
        {
            if (await gpxService.TryApplyGpxTrackToFile(gpxTrack, mediaFile))
            {
                modifiedFiles.Add(mediaFile);
            }
        });

        Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.GeoTag));

        return (result, modifiedFiles.Count);
    }

}
