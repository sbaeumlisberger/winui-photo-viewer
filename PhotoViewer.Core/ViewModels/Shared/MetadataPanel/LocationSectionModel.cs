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
using System.Globalization;
using Windows.System;

namespace PhotoViewer.Core.ViewModels;

public partial class LocationSectionModel : MetadataPanelSectionModelBase
{
    public partial string DisplayText { get; private set; } = "";

    public partial string PlaceholderText { get; private set; } = Strings.MetadataPanel_LocationPlaceholder;

    private partial bool CanShowLocationOnMap { get; set; } = false;

    private Location? location;

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

        var address = allLocationEqual ? addresses.FirstOrDefault()?.ToAddress() : null;
        var geoPoint = allLocationEqual ? geoPoints.FirstOrDefault()?.ToGeoPoint() : null;  
        bool locationAvailable = address is not null || geoPoint is not null;
        location = locationAvailable ? new Location(address, geoPoint) : null;

        PlaceholderText = allLocationEqual
            ? Strings.MetadataPanel_LocationPlaceholder
            : Strings.MetadataPanel_LocationPlaceholderDifferentValues;

        CanShowLocationOnMap = location is not null;

        await UpdateDisplayTextAsync(location, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanShowLocationOnMap))]
    private async Task ShowLocationOnMapAsync()
    {
        if (location!.GeoPoint is not null)
        {
            string latitude = location.GeoPoint.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitude = location.GeoPoint.Longitude.ToString(CultureInfo.InvariantCulture);      
            await Launcher.LaunchUriAsync(new Uri($"https://www.google.com/maps?q={latitude},{longitude}"));
        }
        else 
        {
            await Launcher.LaunchUriAsync(new Uri($"https://www.google.com/maps?q={location.Address}"));
        }
    }

    [RelayCommand]
    private async Task EditLocationAsync()
    {
        var dialogModel = viewModelFactory.CreateEditLocationDialogModel(location, SaveLocationAsync);
        await dialogService.ShowDialogAsync(dialogModel);
    }

    [RelayCommand]
    private async Task ImportFromGpxFileAsync()
    {
        await dialogService.ShowDialogAsync(new ImportGpxTrackDialogModel(Messenger, dialogService, gpxService, Files));
    }

    [RelayCommand]
    private async Task CopyFormOtherPhotoAsync()
    {
        var filePickerModel = new FileOpenPickerModel2()
        {
            FileTypeFilter = [.. BitmapFileInfo.JpegFileExtensions, .. BitmapFileInfo.TiffFileExtensions],
            InitialFolder = Path.GetDirectoryName(Files[0].FilePath)
        };

        await dialogService.ShowDialogAsync(filePickerModel);

        if (filePickerModel.FilePath is { } file)
        {
            try
            {
                using var fileStream = File.OpenRead(file);
                var metadataDecoder = new MetadataDecoder(fileStream);
                var address = metadataDecoder.GetProperty(MetadataProperties.Address)?.ToAddress();
                var geoTag = metadataDecoder.GetProperty(MetadataProperties.GeoTag)?.ToGeoPoint();
                await SaveLocationAsync(new Location(address, geoTag));
            }
            catch (Exception ex)
            {
                Log.Error("Failed to copy location from other photo", ex);

                await dialogService.ShowDialogAsync(new MessageDialogModel()
                {
                    Title = Strings.MetadataPanel_CopyLocationFailed,
                    Message = ex.Message,
                });
            }
        }
    }

    private async Task SaveLocationAsync(Location? location)
    {
        await WriteFilesAsync(async file =>
        {
            await metadataService.WriteMetadataAsync(file, MetadataProperties.Address, location?.Address?.ToAddressTag()).ConfigureAwait(false);
            await metadataService.WriteMetadataAsync(file, MetadataProperties.GeoTag, location?.GeoPoint?.ToGeoTag()).ConfigureAwait(false);
        },
        processedFiles =>
        {
            Messenger.Send(new MetadataModifiedMessage(processedFiles, MetadataProperties.Address));
            Messenger.Send(new MetadataModifiedMessage(processedFiles, MetadataProperties.GeoTag));
        });
    }

    private async Task UpdateDisplayTextAsync(Location? location, CancellationToken cancellationToken)
    {
        if(location is null)
        {
            DisplayText = "";
        }
        else if (location.Address is not null && location.GeoPoint is not null)
        {
            DisplayText = location.Address.ToString() + " (" + location.GeoPoint.ToDecimalString() + ")";
        }
        else if (location.GeoPoint is not null)
        {
            DisplayText = location.GeoPoint.ToDecimalString();

            try
            {
                var address = await locationService.FindAddressAsync(location.GeoPoint);
                cancellationToken.ThrowIfCancellationRequested();
                if (address is not null)
                {
                    DisplayText = address.ToString() + " (" + location.GeoPoint.ToDecimalString() + ")";
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error("Failed to find address for geopoint " + location.GeoPoint.ToDecimalString(), ex);
            }
        }
        else if (location.Address is not null)
        {
            DisplayText = location.Address.ToString();
        }
    }

}
