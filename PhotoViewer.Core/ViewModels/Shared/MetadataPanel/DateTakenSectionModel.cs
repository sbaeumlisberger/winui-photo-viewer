using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Diagnostics;

namespace PhotoViewer.Core.ViewModels;

public partial class DateTakenSectionModel : MetadataPanelSectionModelBase
{
    public bool IsNotPresent { get; private set; }

    public bool IsSingleValue { get; private set; }

    public bool IsRange { get; private set; }

    public DateTimeOffset? Date { get; set; }

    public TimeSpan? Time { get; set; }

    public string RangeText { get; private set; } = "";

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private bool isUpdating = false;

    internal DateTakenSectionModel(
        IMessenger messenger,
        IMetadataService metadataService,
        IDialogService dialogService,
        IBackgroundTaskService backgroundTaskService) : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.dialogService = dialogService;
    }

    protected override void OnFilesChanged(IReadOnlyList<MetadataView> metadata)
    {
        Update(metadata);
    }

    protected override void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == MetadataProperties.DateTaken)
        {
            Update(metadata);
        }
    }

    private void Update(IReadOnlyList<MetadataView> metadata)
    {
        isUpdating = true;

        var values = metadata.Select(m => m.Get(MetadataProperties.DateTaken)).ToList();

        IsNotPresent = values.All(dateTaken => dateTaken is null);
        IsSingleValue = !IsNotPresent && values.All(dateTaken => dateTaken == values.FirstOrDefault());
        IsRange = !IsNotPresent && !IsSingleValue;

        if (IsSingleValue)
        {
            var dateTaken = values.FirstOrDefault();
            Date = dateTaken?.Date;
            Time = dateTaken?.TimeOfDay;
        }
        else
        {
            Date = null;
            Time = null;
        }

        if (IsRange)
        {
            var datesTaken = values.OfType<DateTime>().ToList();
            RangeText = FormatDate(datesTaken.Min()) + " - " + FormatDate(datesTaken.Max());
        }
        else
        {
            RangeText = "";
        }

        isUpdating = false;
    }

    partial void OnDateChanged()
    {
        OnDateOrTimeChanged();
    }

    partial void OnTimeChanged()
    {
        OnDateOrTimeChanged();
    }

    private async void OnDateOrTimeChanged()
    {
        if (isUpdating)
        {
            return;
        }

        Debug.Assert(IsSingleValue);
        Debug.Assert(Date is not null);
        Debug.Assert(Time is not null);

        var dateTaken = ToDateTaken(Date.Value.LocalDateTime, Time.Value);
        await WriteFilesAsync(dateTaken);
    }

    [RelayCommand(CanExecute = nameof(IsNotPresent))]
    private async Task AddDateTakenAsync()
    {
        isUpdating = true;
        var dateTaken = DateTime.Now;
        Date = dateTaken;
        Time = dateTaken.TimeOfDay;
        IsNotPresent = false;
        IsSingleValue = true;
        isUpdating = false;

        await WriteFilesAsync(dateTaken);
    }

    [RelayCommand]
    private async Task ShiftDateTakenAsync()
    {
        await dialogService.ShowDialogAsync(new ShiftDatenTakenDialogModel(Messenger, metadataService, Files));
    }

    private string FormatDate(DateTimeOffset date)
    {
        return date.ToString("g");
    }

    private DateTime ToDateTaken(DateTime date, TimeSpan time)
    {
        return new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, time.Milliseconds, time.Microseconds);
    }

    private async Task WriteFilesAsync(DateTime? dateTaken)
    {
        await WriteFilesAndCancelPreviousAsync(async (file, cancellationToken) =>
        {
            if (!Equals(dateTaken, await metadataService.GetMetadataAsync(file, MetadataProperties.DateTaken).ConfigureAwait(false)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await metadataService.WriteMetadataAsync(file, MetadataProperties.DateTaken, dateTaken).ConfigureAwait(false);
            }
        },
        processedFiles => Messenger.Send(new MetadataModifiedMessage(processedFiles, MetadataProperties.DateTaken)));
    }

}
