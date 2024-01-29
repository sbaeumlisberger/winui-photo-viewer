using MetadataAPI.Data;
using System.Globalization;
using Windows.Devices.Geolocation;

namespace PhotoViewer.Core.Utils;

public static class GeopointExtension
{
    public static GeoTag ToGeoTag(this Geopoint geopoint)
    {
        return new GeoTag()
        {
            Latitude = geopoint.Position.Latitude,
            Longitude = geopoint.Position.Longitude,
            Altitude = geopoint.Position.Altitude,
            AltitudeReference = (AltitudeReference)geopoint.AltitudeReferenceSystem,
        };
    }

    public static string ToDecimalString(this Geopoint geopoint, int decimals = 6)
    {
        return geopoint.Position.Latitude.ToString("N" + decimals, CultureInfo.InvariantCulture)
            + ", " + geopoint.Position.Longitude.ToString("N" + decimals, CultureInfo.InvariantCulture);
    }
}
