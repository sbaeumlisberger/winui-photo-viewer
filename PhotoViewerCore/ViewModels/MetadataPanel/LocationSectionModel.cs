using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using System.Globalization;
using Windows.Devices.Geolocation;
using Windows.System;

namespace MetadataEditModule.ViewModel
{

    public partial class LocationSectionModel : ViewModelBase
    {
        private const string LocationMarker = "Aufnahmeort"; // TODO translate

        public AddressTag? AddressTag { get; private set; }

        public GeoTag? GeoTag { get; private set; }

        public string DisplayText { get; private set; } = "";

        private bool CanShowLocationOnMap { get; set; } = false;

        private Location? location;

        private readonly ILocationService locationService;
        private readonly IDialogService dialogService;

        public LocationSectionModel(ILocationService locationService, IDialogService dialogService)
        {
            this.locationService = locationService;
            this.dialogService = dialogService;
        }

        public async void UpdateAsync(IList<MetadataView> metadata, CancellationToken cancellationToken)
        {
            var addresses = metadata.Select(m => m.Get(MetadataProperties.Address));
            var geoPoints = metadata.Select(m => m.Get(MetadataProperties.GeoTag));

            bool allAddressesEqual = addresses.All(x => Equals(x, addresses.First()));
            bool allGeoTagsEqual = geoPoints.All(x => Equals(x, geoPoints.First()));
            bool allLocationEqual = allAddressesEqual && allGeoTagsEqual;

            AddressTag = allLocationEqual ? addresses.FirstOrDefault() : null;
            GeoTag = allLocationEqual ? geoPoints.FirstOrDefault() : null;

            location = new Location(AddressTag?.ToAddress(), GeoTag?.ToGeopoint());
            Update(location);

            var completedLocation = await TryGetCompletedLocationAsync(location, cancellationToken);

            if (completedLocation is not null && completedLocation != location)
            {
                location = completedLocation;
                Update(completedLocation);
            }
        }

        public void Clear()
        {
            AddressTag = null;
            GeoTag = null;
            CanShowLocationOnMap = false;
            DisplayText = "";
        }

        [RelayCommand(CanExecute = nameof(CanShowLocationOnMap))]
        private async Task ShowLocationOnMapAsync()
        {
            var geopoint = location!.Point!;
            string latitude = geopoint.Position.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitude = geopoint.Position.Longitude.ToString(CultureInfo.InvariantCulture);
            Uri uri = new Uri(@"bingmaps:?collection=point." + latitude + "_" + longitude + "_" + LocationMarker + "&sty=a");
            await Launcher.LaunchUriAsync(uri, new LauncherOptions
            {
                TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe"
            });
        }


        [RelayCommand]
        private async Task EditLocationAsync()
        {
            await dialogService.ShowDialogAsync(new EditLocationDialogModel());
        }

        private void Update(Location location)
        {
            CanShowLocationOnMap = location.Point is not null;
            DisplayText = CreateDisplayText(location);
        }

        private async Task<Location?> TryGetCompletedLocationAsync(Location location, CancellationToken cancellationToken)
        {
            if (location.Point is null && location.Address is not null)
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
            else if (location.Address is null && location.Point is not null)
            {
                try
                {
                    var address = await locationService.FindAddressAsync(location.Point);
                    cancellationToken.ThrowIfCancellationRequested();
                    return new Location(address, location.Point);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to find address for geo tag " + ToDecimalFormattedString(location.Point), ex);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        private string CreateDisplayText(Location location)
        {
            if (location.Address is not null && location.Point is not null)
            {
                return location.Address.ToString() + " (" + ToDecimalFormattedString(location.Point) + ")";
            }
            else if (location.Address is not null)
            {
                return location.Address.ToString();
            }
            else if (location.Point is not null)
            {
                return ToDecimalFormattedString(location.Point);
            }
            else
            {
                return "";
            }
        }

        private string ToDecimalFormattedString(Geopoint geopoint, int decimals = 4)
        {
            return geopoint.Position.Latitude.ToString("N" + decimals) + "°"
                + " " + geopoint.Position.Longitude.ToString("N" + decimals) + "°";
        }

    }
}
