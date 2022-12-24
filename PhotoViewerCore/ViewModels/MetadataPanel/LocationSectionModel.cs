using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Messages;
using PhotoViewerCore.Models;
using PhotoViewerCore.Resources;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using Tocronx.SimpleAsync;
using Windows.System;

namespace MetadataEditModule.ViewModel
{
    public partial class LocationSectionModel : MetadataPanelSectionModelBase
    {
        public AddressTag? AddressTag { get; private set; }

        public GeoTag? GeoTag { get; private set; }

        public string DisplayText { get; private set; } = "";

        public string PlaceholderText { get; private set; } = Strings.MetadataPanel_LocationPlaceholder;

        private bool CanShowLocationOnMap { get; set; } = false;

        private Location? orginalLocation;
        private Location? completedLocation;

        private readonly IMetadataService metadataService;
        private readonly ILocationService locationService;
        private readonly IDialogService dialogService;
        private readonly IClipboardService clipboardService;

        private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

        public LocationSectionModel(
            SequentialTaskRunner writeFilesRunner,
            IMetadataService metadataService,
            ILocationService locationService, 
            IDialogService dialogService,
            IClipboardService clipboardService,
            IMessenger messenger) : base(writeFilesRunner, messenger)
        {
            this.metadataService = metadataService;
            this.locationService = locationService;
            this.dialogService = dialogService;
            this.clipboardService = clipboardService;
        }

        protected override void OnFilesChanged(IList<MetadataView> metadata)
        {
            updateRunner.RunAndCancelPrevious(cancellationToken => UpdateAsync(metadata, cancellationToken));
        }

        protected override void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty)
        {
            if(metadataProperty == MetadataProperties.Address || metadataProperty == MetadataProperties.GeoTag) 
            {
                updateRunner.RunAndCancelPrevious(cancellationToken => UpdateAsync(metadata, cancellationToken));
            }
        }

        private async Task UpdateAsync(IList<MetadataView> metadata, CancellationToken cancellationToken) 
        {
            var addresses = metadata.Select(m => m.Get(MetadataProperties.Address));
            var geoPoints = metadata.Select(m => m.Get(MetadataProperties.GeoTag));

            bool allAddressesEqual = addresses.All(x => Equals(x, addresses.First()));
            bool allGeoTagsEqual = geoPoints.All(x => Equals(x, geoPoints.First()));
            bool allLocationEqual = allAddressesEqual && allGeoTagsEqual;

            AddressTag = allLocationEqual ? addresses.FirstOrDefault() : null;
            GeoTag = allLocationEqual ? geoPoints.FirstOrDefault() : null;
            PlaceholderText = allLocationEqual 
                ? Strings.MetadataPanel_LocationPlaceholder 
                : Strings.MetadataPanel_LocationPlaceholderMultipleValues;

            orginalLocation = new Location(AddressTag?.ToAddress(), GeoTag?.ToGeopoint());
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
            string latitude = geopoint.Position.Latitude.ToInvariantString();
            string longitude = geopoint.Position.Longitude.ToInvariantString();
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
            var dialogModel = new EditLocationDialogModel(orginalLocation, SaveLocation, locationService, clipboardService);
            await dialogService.ShowDialogAsync(dialogModel);
        }

        private async Task SaveLocation(Location? location)
        {
            await EnqueueWriteFiles(async (files) =>
            {
                var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
                {
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.Address,
                        location?.Address?.ToAddressTag()).ConfigureAwait(false);
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.GeoTag,
                        location?.Geopoint?.ToGeoTag()).ConfigureAwait(false);
                });

                Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.Address));
                Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.GeoTag));

                if (result.HasFailures)
                {
                    // TODO show error message
                }
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
                    var geopoint = await locationService.FindGeopointAsync(location.Address.ToString());
                    cancellationToken.ThrowIfCancellationRequested();
                    return new Location(location.Address, geopoint);
                }
                catch (Exception ex)
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
                catch (Exception ex)
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
}
