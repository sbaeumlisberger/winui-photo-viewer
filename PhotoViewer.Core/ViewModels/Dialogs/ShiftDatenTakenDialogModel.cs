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

public partial class ShiftDatenTakenDialogModel : ViewModelBase
{
    public bool ShowInput { get; private set; } = true;
    public bool ShowProgress { get; private set; } = false;
    public bool ShowResult { get; private set; } = false;

    public int Days { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public int Seconds { get; set; }

    public Progress? Progress { get; private set; }

    public IReadOnlyList<string> Errors { get; private set; } = new List<string>();

    public bool IsCompletedSuccessfully { get; private set; } = false;

    public bool IsCompletedWithErrors { get; private set; } = false;

    private readonly IMessenger messenger;

    private readonly IMetadataService metadataService;

    private readonly IReadOnlyCollection<IMediaFileInfo> mediaFiles;

    public ShiftDatenTakenDialogModel(IMessenger messenger, IMetadataService metadataService, IReadOnlyCollection<IMediaFileInfo> mediaFiles)
    {
        this.messenger = messenger;
        this.metadataService = metadataService;
        this.mediaFiles = mediaFiles;
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        var cts = new CancellationTokenSource();
        Progress = new Progress(cts);

        ShowInput = false;
        ShowProgress = true;

        var timeSpan = TimeSpan.FromDays(Days)
            + TimeSpan.FromHours(Hours)
            + TimeSpan.FromMinutes(Minutes)
            + TimeSpan.FromSeconds(Seconds);

        try
        {
            var result = await ShiftDateTakenAsync(mediaFiles, timeSpan, Progress, cts.Token);

            IsCompletedSuccessfully = result.IsSuccessfully;
            IsCompletedWithErrors = result.HasFailures;
            Errors = result.Failures.Select(failure => failure.Element.FileName + ": " + failure.Exception.Message).ToList();
            ShowProgress = false;
            ShowResult = true;
        }
        catch (OperationCanceledException)
        {
            // canceld by user
        }
        catch (Exception ex)
        {
            Log.Error("Failed to shift date taken", ex);
            IsCompletedWithErrors = true;
            Errors = new List<string> { ex.Message };
            ShowProgress = false;
            ShowResult = true;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Progress?.Cancel();
    }

    private async Task<ParallelResult<IBitmapFileInfo>> ShiftDateTakenAsync(IReadOnlyCollection<IMediaFileInfo> mediaFiles, TimeSpan timeSpan, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var filesToShift = mediaFiles.OfType<IBitmapFileInfo>().Where(bitmap => bitmap.IsMetadataSupported).ToList();

        var result = await filesToShift.Parallel(cancellationToken, progress).TryProcessAsync(async file =>
        {
            if (await metadataService.GetMetadataAsync(file, MetadataProperties.DateTaken) is { } dateTaken)
            {
                var shiftedDateTaken = dateTaken.Add(timeSpan);
                await metadataService.WriteMetadataAsync(file, MetadataProperties.DateTaken, shiftedDateTaken);
            }
        });

        messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.DateTaken));

        return result;
    }
}
