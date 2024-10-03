namespace PhotoViewer.Core.Models;

public record Location
{
    public Address? Address { get; }

    public GeoPoint? GeoPoint { get; }

    public Location(Address? address, GeoPoint? geoPoint)
    {
        Address = address;
        GeoPoint = geoPoint;
    }

    public override string ToString()
    {
        return Address != null ? Address.ToString() : GeoPoint?.ToDecimalString() ?? "";
    }
}
