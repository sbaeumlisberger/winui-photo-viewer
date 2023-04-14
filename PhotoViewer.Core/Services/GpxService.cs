using MetadataAPI.Data;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using System.Globalization;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Devices.Geolocation;

namespace PhotoViewer.Core.Services;

internal interface IGpxService 
{
    Task<List<GpxTrackPoint>> ReadTrackFromGpxFileAsync(IStorageFile file);
    GpxTrackPoint? FindTrackPointForTimestamp(IEnumerable<GpxTrackPoint> gpxTrack, DateTimeOffset timestamp);
}

internal class GpxService : IGpxService
{
    public async Task<List<GpxTrackPoint>> ReadTrackFromGpxFileAsync(IStorageFile file)
    {
        string gpxData = await FileIO.ReadTextAsync(file);
        return ParseGpxTrack(gpxData);
    }

    public List<GpxTrackPoint> ParseGpxTrack(string gpxData)
    {
        var trkpList = new List<GpxTrackPoint>();
        
        XNamespace ns = "http://www.topografix.com/GPX/1/1";
        XDocument gpxDoc = XDocument.Parse(gpxData);
        XElement trkElement = gpxDoc.Root!.Element(ns + "trk")!;
        IEnumerable<XElement> trksegElements = trkElement.Elements(ns + "trkseg");

        foreach (XElement trksegElement in trksegElements)
        {
            IEnumerable<XElement> trkptElements = trksegElement.Elements(ns + "trkpt");

            foreach (XElement trkptElement in trkptElements)
            {
                GpxTrackPoint trkpt = new GpxTrackPoint();

                trkpt.Latitude = double.Parse(trkptElement.Attribute("lat")!.Value, CultureInfo.InvariantCulture);
                trkpt.Longitude = double.Parse(trkptElement.Attribute("lon")!.Value, CultureInfo.InvariantCulture);

                if (trkptElement.Element(ns + "ele") is { } eleElement)
                {
                    trkpt.Ele = double.Parse(eleElement.Value, CultureInfo.InvariantCulture);
                }

                if (trkptElement.Element(ns + "time") is { } timeElement)
                {
                    trkpt.Time = DateTimeOffset.Parse(timeElement.Value, CultureInfo.InvariantCulture);
                }

                trkpList.Add(trkpt);
            }
        }
        return trkpList;
    }

    public GpxTrackPoint? FindTrackPointForTimestamp(IEnumerable<GpxTrackPoint> gpxTrack, DateTimeOffset timestamp)
    {
        var trackPointsWithTime = gpxTrack.Where(trkpt => trkpt.Time != null).ToList();

        if (timestamp < trackPointsWithTime.First().Time || timestamp > trackPointsWithTime.Last().Time)
        {
            return null;
        }

        return trackPointsWithTime.MinBy(trkpt => (timestamp - trkpt.Time!.Value).Duration());
    }

}
