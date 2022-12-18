using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Models;
using PostSharp.Patterns.Model;
using Windows.Devices.Geolocation;

namespace PhotoViewerCore.ViewModels;

public partial class EditLocationDialogDetailsTabModel : ViewModelBase
{
    public event EventHandler<Location?>? LocationDetailsChanged;

    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Latitude { get; set; } = string.Empty;

    public string Longitude { get; set; } = string.Empty;

    public string Altitude { get; set; } = string.Empty;

    public AltitudeReferenceSystem AltitudeReferenceSystem { get; set; } = AltitudeReferenceSystem.Unspecified;

    public IList<AltitudeReferenceSystem> AvailableAltitudeReferenceSystems => Enum.GetValues<AltitudeReferenceSystem>();

    private bool isUpdating = false;

    public EditLocationDialogDetailsTabModel() 
    {
        PropertyChanged += EditLocationDialogDetailsTabModel_PropertyChanged;
    }

    private void EditLocationDialogDetailsTabModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!isUpdating && TryParseLocation(out var location))
        {
            LocationDetailsChanged?.Invoke(this, location);
        }
    }

    public void Update(Location? location)
    {
        isUpdating = true;
        Street = location?.Address?.Street ?? string.Empty;
        City = location?.Address?.City ?? string.Empty;
        Region = location?.Address?.Region ?? string.Empty;
        Country = location?.Address?.Country ?? string.Empty;
        Latitude = location?.Point?.Position.Latitude.ToString() ?? string.Empty;
        Longitude = location?.Point?.Position.Longitude.ToString() ?? string.Empty;
        Altitude = location?.Point?.Position.Altitude.ToString() ?? string.Empty;
        AltitudeReferenceSystem = location?.Point?.AltitudeReferenceSystem ?? AltitudeReferenceSystem.Unspecified;
        NotifyPropertyChangedServices.RaiseEventsImmediate(this);
        isUpdating = false;
    }

    public bool TryParseLocation(out Location? location)
    {
        try
        {
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
            if (!string.IsNullOrEmpty(Latitude) && !string.IsNullOrEmpty(Longitude))
            {
                geopoint = new Geopoint(new BasicGeoposition()
                {
                    Latitude = double.Parse(Latitude),
                    Longitude = double.Parse(Longitude),
                    Altitude = !string.IsNullOrEmpty(Altitude) ? double.Parse(Altitude) : 0,
                }, AltitudeReferenceSystem);
            }

            if (address is not null && geopoint is not null)
            {
                location = new Location(address, geopoint);
            }
            else
            {
                location = null;
            }         
            return true;
        }
        catch(Exception ex)
        {
            Log.Error("Failed to process input", ex); // TODO
            location = null;
            return false;
        }
    }
}