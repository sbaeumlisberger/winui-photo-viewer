using MetadataAPI.Data;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Utils;

public static class GeoTagExtension
{
    public static GeoPoint ToGeoPoint(this GeoTag geoTag)
    {
        return new GeoPoint(geoTag.Latitude, geoTag.Longitude, geoTag.Altitude);
    }
}
