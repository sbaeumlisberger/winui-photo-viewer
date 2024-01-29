using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Globalization;
using Windows.System;

namespace MetadataEditModule.ViewModel;

public partial class LocationSectionModel : MetadataPanelSectionModelBase
{
    public string DisplayText { get; private set; } = "";

    public string PlaceholderText { get; private set; } = Strings.MetadataPanel_LocationPlaceholder;

    private bool CanShowLocationOnMap { get; set; } = false;

    private Location? orginalLocation;
    private Location? completedLocation;

    private readonly IMetadataService metadataService;
    private readonly ILocationService locationService;
    private readonly IDialogService dialogService;
    private readonly IViewModelFactory viewModelFactory;
    private readonly IGpxService gpxService;

    private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

    internal LocationSectionModel(
        IMetadataService metadataService,
        ILocationService locationService,
        IDialogService dialogService,
        IViewModelFactory viewModelFactory,
        IGpxService gpxService,
        IMessenger messenger,
        IBackgroundTaskService backgroundTaskService) : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.locationService = locationService;
        this.dialogService = dialogService;
        this.viewModelFactory = viewModelFactory;
        this.gpxService = gpxService;
    }

    protected override void OnFilesChanged(IReadOnlyList<MetadataView> metadata)
    {
        updateRunner.RunAndCancelPrevious(cancellationToken => UpdateAsync(metadata, cancellationToken));
    }

    protected override void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == MetadataProperties.Address || metadataProperty == MetadataProperties.GeoTag)
        {
            updateRunner.RunAndCancelPrevious(cancellationToken => UpdateAsync(metadata, cancellationToken));
        }
    }

    private async Task UpdateAsync(IReadOnlyList<MetadataView> metadata, CancellationToken cancellationToken)
    {
        var addresses = metadata.Select(m => m.Get(MetadataProperties.Address));
        var geoPoints = metadata.Select(m => m.Get(MetadataProperties.GeoTag));

        bool allAddressesEqual = addresses.All(x => Equals(x, addresses.First()));
        bool allGeoTagsEqual = geoPoints.All(x => Equals(x, geoPoints.First()));
        bool allLocationEqual = allAddressesEqual && allGeoTagsEqual;

        var addressTag = allLocationEqual ? addresses.FirstOrDefault() : null;
        var geoTag = allLocationEqual ? geoPoints.FirstOrDefault() : null;
        PlaceholderText = allLocationEqual
            ? Strings.MetadataPanel_LocationPlaceholder
            : Strings.MetadataPanel_LocationPlaceholderMultipleValues;

        orginalLocation = new Location(addressTag?.ToAddress(), geoTag?.ToGeopoint());
        UpdateForLocation(orginalLocation);

        this.completedLocation = orginalLocation;
        var completedLocation = await TryGetCompletedLocationAsync(orginalLocation, cancellationToken);
        if (completedLocation is not null && completedLocation != orginalLocation)
        {
            this.completedLocation = completedLocation;
            UpdateForLocation(completedLocation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowLocationOnMap))]
    private async Task ShowLocationOnMapAsync()
    {
        var geopoint = completedLocation!.Geopoint!;
        string latitude = geopoint.Position.Latitude.ToString(CultureInfo.InvariantCulture);
        string longitude = geopoint.Position.Longitude.ToString(CultureInfo.InvariantCulture);
        string description = completedLocation?.Address?.ToString() ?? geopoint.ToDecimalString(); ;
        Uri uri = new Uri(@"bingmaps:?collection=point." + latitude + "_" + longitude + "_" + description + "&sty=a");
        await Launcher.LaunchUriAsync(uri, new LauncherOptions
        {
            TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe"
        });
    }


    [RelayCommand]
    private async Task EditLocationAsync()
    {
        var dialogModel = viewModelFactory.CreateEditLocationDialogModel(orginalLocation, SaveLocation);
        await dialogService.ShowDialogAsync(dialogModel);
    }

    [RelayCommand]
    private async Task ImportFromGpxFileAsync()
    {
        var fileOpenPickerModel = new FileOpenPickerModel()
        {
            FileTypeFilter = new[] { ".gpx" }
        };

        await dialogService.ShowDialogAsync(fileOpenPickerModel);

        if (fileOpenPickerModel.File is { } gpxFile)
        {
            GpxTrack gpxTrack;
            try
            {
                gpxTrack = await gpxService.ReadTrackFromGpxFileAsync(gpxFile);
            }
            catch (Exception ex)
            {
                await dialogService.ShowDialogAsync(new MessageDialogModel()
                {
                    Title = Strings.GpxFileParseErrorDialog_Title,
                    Message = string.Format(Strings.GpxFileParseErrorDialog_Message, ex.Message)
                });
                return;
            }

            var modifiedFiles = new List<IBitmapFileInfo>();

            await WriteFilesAndCancelPreviousAsync(async (file, _) =>
            {
                if (await gpxService.TryApplyGpxTrackToFile(gpxTrack, file))
                {
                    modifiedFiles.Add(file);
                }
            },
            _ => Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.GeoTag)));
        }
    }

    private async Task SaveLocation(Location? location)
    {
        await WriteFilesAsync(async file =>
        {
            await metadataService.WriteMetadataAsync(file, MetadataProperties.Address, location?.Address?.ToAddressTag()).ConfigureAwait(false);
            await metadataService.WriteMetadataAsync(file, MetadataProperties.GeoTag, location?.Geopoint?.ToGeoTag()).ConfigureAwait(false);
        },
        processedFiles =>
        {
            Messenger.Send(new MetadataModifiedMessage(processedFiles, MetadataProperties.Address));
            Messenger.Send(new MetadataModifiedMessage(processedFiles, MetadataProperties.GeoTag));
        });
    }

    private void UpdateForLocation(Location location)
    {
        CanShowLocationOnMap = location.Geopoint is not null;
        DisplayText = CreateDisplayText(location);
    }

    private async Task<Location?> TryGetCompletedLocationAsync(Location location, CancellationToken cancellationToken)
    {
        if (location.Geopoint is null && location.Address is not null)
        {
            try
            {
                var geopoint = await locationService.FindGeopointAsync(location.Address);
                cancellationToken.ThrowIfCancellationRequested();
                return new Location(location.Address, geopoint);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error("Failed to find geo point for address " + location.Address.ToString(), ex);
            }
        }
        else if (location.Address is null && location.Geopoint is not null)
        {
            try
            {
                var address = await locationService.FindAddressAsync(location.Geopoint);
                cancellationToken.ThrowIfCancellationRequested();
                return new Location(address, location.Geopoint);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error("Failed to find address for geopoint " + location.Geopoint.ToDecimalString(), ex);
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return null;
    }

    private string CreateDisplayText(Location location)
    {
        if (location.Address is not null && location.Geopoint is not null)
        {
            return location.Address.ToString() + " (" + location.Geopoint.ToDecimalString() + ")";
        }
        else if (location.Address is not null)
        {
            return location.Address.ToString();
        }
        else if (location.Geopoint is not null)
        {
            return location.Geopoint.ToDecimalString();
        }
        else
        {
            return "";
        }
    }

}
