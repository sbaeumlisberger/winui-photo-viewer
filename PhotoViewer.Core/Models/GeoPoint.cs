using System.Globalization;

namespace PhotoViewer.Core.Models;

public record GeoPoint
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double? Altitude { get; }

    public GeoPoint(double latitude, double longitude, double? altitude = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
    }

    public string ToDecimalString(int decimals = 6)
    {
        return Latitude.ToString("N" + decimals, CultureInfo.InvariantCulture)
            + ", " + Longitude.ToString("N" + decimals, CultureInfo.InvariantCulture);
    }
}
