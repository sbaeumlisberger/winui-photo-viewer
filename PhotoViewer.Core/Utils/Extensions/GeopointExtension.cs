using MetadataAPI.Data;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Utils;

public static class GeoPointExtension
{
    public static GeoTag ToGeoTag(this GeoPoint geopoint)
    {
        return new GeoTag()
        {
            Latitude = geopoint.Latitude,
            Longitude = geopoint.Longitude,
            Altitude = geopoint.Altitude,
        };
    }
}
