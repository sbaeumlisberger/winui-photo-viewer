using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using System.Globalization;
using Windows.Devices.Geolocation;

namespace PhotoViewer.Core.ViewModels;

public partial class EditLocationDialogDetailsTabModel : ViewModelBase
{
    public event EventHandler<Location?>? LocationChanged;

    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Latitude { get; set; } = string.Empty;

    public string Longitude { get; set; } = string.Empty;

    public string Altitude { get; set; } = string.Empty;

    public AltitudeReferenceSystem AltitudeReferenceSystem { get; set; } = AltitudeReferenceSystem.Unspecified;

    public IList<AltitudeReferenceSystem> AvailableAltitudeReferenceSystems => Enum.GetValues<AltitudeReferenceSystem>();

    public bool CanSave { get; set; } = true;

    private bool CanCopyCoordinates => Latitude.Trim() != string.Empty && Longitude.Trim() != string.Empty;

    private readonly IClipboardService clipboardService;

    private Location? location;

    private bool isUpdating = false;

    public EditLocationDialogDetailsTabModel(IClipboardService clipboardService)
    {
        this.clipboardService = clipboardService;
        PropertyChanged += EditLocationDialogDetailsTabModel_PropertyChanged;
    }

    private void EditLocationDialogDetailsTabModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!isUpdating)
        {
            ValidateAndParseLocation();
        }
    }

    public void Update(Location? location)
    {
        if (location == this.location)
        {
            return;
        }
        isUpdating = true;
        this.location = location;
        Street = location?.Address?.Street ?? string.Empty;
        City = location?.Address?.City ?? string.Empty;
        Region = location?.Address?.Region ?? string.Empty;
        Country = location?.Address?.Country ?? string.Empty;
        Latitude = location?.Geopoint?.Position.Latitude.ToInvariantString() ?? string.Empty;
        Longitude = location?.Geopoint?.Position.Longitude.ToInvariantString() ?? string.Empty;
        Altitude = location?.Geopoint?.Position.Altitude.ToString() ?? string.Empty;
        AltitudeReferenceSystem = location?.Geopoint?.AltitudeReferenceSystem ?? AltitudeReferenceSystem.Unspecified;
        CanSave = true;
        isUpdating = false;
    }

    [RelayCommand(CanExecute = nameof(CanCopyCoordinates))]
    public void CopyCoordinates()
    {
        clipboardService.CopyText(Latitude + ", " + Longitude);
    }

    private void ValidateAndParseLocation()
    {
        bool valid = true;

        Address? address = null;

        if (!string.IsNullOrEmpty(Latitude) || !string.IsNullOrEmpty(City)
            || !string.IsNullOrEmpty(Region) || !string.IsNullOrEmpty(Country))
        {
            address = new Address()
            {
                Street = Street,
                City = City,
                Region = Region,
                Country = Country,
            };
        }

        Geopoint? geopoint = null;

        if (AnyFieldFilled(Latitude, Longitude, Altitude))
        {
            if (double.TryParse(Latitude, CultureInfo.InvariantCulture, out double latitude)
                && latitude >= -90 && latitude <= 90
                && double.TryParse(Longitude, CultureInfo.InvariantCulture, out double longitude)
                && longitude >= -180 && longitude <= 180
                && (double.TryParse(Altitude, out double altitude) || string.IsNullOrEmpty(Altitude)))
            {
                geopoint = new Geopoint(new BasicGeoposition()
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = altitude,
                }, AltitudeReferenceSystem);
            }
            else
            {
                valid = false;
            }
        }

        Location? location = null;

        if (address is not null || geopoint is not null)
        {
            location = new Location(address, geopoint);
        }

        if (location != this.location)
        {
            this.location = location;
            LocationChanged?.Invoke(this, location);
        }

        CanSave = valid;
    }

    private bool AnyFieldFilled(params string[] fields)
    {
        return fields.Any(field => !string.IsNullOrEmpty(field));
    }
}