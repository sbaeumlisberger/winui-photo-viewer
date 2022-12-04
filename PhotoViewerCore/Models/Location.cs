using Windows.Devices.Geolocation;

namespace PhotoViewerCore.Models;

public record Location
{
    public Address? Address { get; }

    public Geopoint? Point { get; }

    public Location(Address? address, Geopoint? point)
    {
        Address = address;
        Point = point;
    }
}
