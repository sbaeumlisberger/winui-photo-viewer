using Windows.Devices.Geolocation;

namespace PhotoViewer.Core.Models;

public record Location
{
    public Address? Address { get; }

    public Geopoint? Geopoint { get; }

    public Location(Address? address, Geopoint? point)
    {
        Address = address;
        Geopoint = point;
    }
}
