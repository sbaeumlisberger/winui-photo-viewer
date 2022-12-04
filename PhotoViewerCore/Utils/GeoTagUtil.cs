using MetadataAPI.Data;
using Windows.Devices.Geolocation;

namespace PhotoViewerCore.Utils;

public static class GeoTagUtil
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
