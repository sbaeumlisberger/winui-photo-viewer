using MetadataAPI.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace PhotoViewerCore.Utils;

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
