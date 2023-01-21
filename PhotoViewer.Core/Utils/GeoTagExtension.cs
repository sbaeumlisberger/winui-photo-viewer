using MetadataAPI.Data;
using Windows.Devices.Geolocation;

namespace PhotoViewerCore.Utils;

public static class GeoTagExtension
{
    public static Geopoint ToGeopoint(this GeoTag geoTag)
    {
        var geoPositon = new BasicGeoposition()
        {
            Latitude = geoTag.Latitude,
            Longitude = geoTag.Longitude,
            Altitude = geoTag.Altitude ?? 0
        };
        return new Geopoint(geoPositon, (AltitudeReferenceSystem)geoTag.AltitudeReference);
    }
}
