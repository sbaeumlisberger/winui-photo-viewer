using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class PrefixFilesByDateDialogModel : ViewModelBase
{
    public bool ShowConfirmation { get; private set; } = true;
    public bool ShowProgress { get; private set; } = false;
    public bool ShowErrorMessage { get; private set; } = false;
    public bool ShowSuccessMessage { get; private set; } = false;

    public Progress? Progress { get; private set; }

    public IReadOnlyList<string> Errors { get; private set; } = new List<string>();


    private readonly IReadOnlyCollection<IMediaFileInfo> mediaFiles;

    private readonly IMetadataService metadataService;

    public PrefixFilesByDateDialogModel(IReadOnlyCollection<IMediaFileInfo> mediaFiles, IMetadataService metadataService, IMessenger messenger) : base(messenger)
    {
        this.mediaFiles = mediaFiles;
        this.metadataService = metadataService;
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
            var result = await NumberFilesByDateAsync(mediaFiles, Progress, cts.Token);

            Messenger.Send(new MediaFilesRenamedMessage(result.ProcessedElements.Select(x => x.File).ToList()));

            ShowSuccessMessage = result.IsSuccessfully;
            ShowErrorMessage = result.HasFailures;
            Errors = result.Failures.Select(failure => failure.Element.File.FileName + ": " + failure.Exception.Message).ToList();
            ShowProgress = false;
        }
        catch (OperationCanceledException)
        {
            Log.Info("Numbering files by date was canceled");
        }
        catch (Exception ex)
        {
            Log.Error("Error while numbering files by date", ex);
            ShowErrorMessage = true;
            Errors = new List<string> { ex.Message };
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<ParallelResult<(IMediaFileInfo File, string NewName)>> NumberFilesByDateAsync(IReadOnlyCollection<IMediaFileInfo> files, IProgress<double> progress, CancellationToken cancellationToken)
    {
        Dictionary<IMediaFileInfo, DateTime> dateTakenDict = new();
        foreach (IMediaFileInfo file in files)
        {
            if (file is IBitmapFileInfo bitmapFile
                && bitmapFile.IsMetadataSupported
                && await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.DateTaken) is { } dateTaken)
            {
                dateTakenDict.Add(file, dateTaken);
            }
            else
            {
                dateTakenDict.Add(file, File.GetLastWriteTime(file.FilePath));
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        int digits = files.Count.ToString().Length;
        var filesToRename = files
            .OrderBy(file => dateTakenDict[file])
            .Select((file, index) => (File: file, NewName: (index + 1).ToString().PadLeft(digits, '0') + "_" + file.FileNameWithoutExtension))
            .ToList();
        cancellationToken.ThrowIfCancellationRequested();

        return await filesToRename.Parallel(cancellationToken, progress).TryProcessAsync(async item =>
        {
            await item.File.RenameAsync(item.NewName);
        });
    }
}
