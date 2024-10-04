using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.Core.Models;
using System.Globalization;
using System.Xml.Linq;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

internal interface IGpxService
{
    Task<GpxTrack> ReadTrackFromGpxFileAsync(IStorageFile file);

    Task<bool> TryApplyGpxTrackToFile(GpxTrack gpxTrack, IBitmapFileInfo file);
}

internal class GpxService : IGpxService
{
    private readonly IMetadataService metadataService;

    public GpxService(IMetadataService metadataService)
    {
        this.metadataService = metadataService;
    }

    public async Task<GpxTrack> ReadTrackFromGpxFileAsync(IStorageFile file)
    {
        string gpxData = await FileIO.ReadTextAsync(file);
        return ParseGpxTrack(gpxData);
    }

    public GpxTrack ParseGpxTrack(string gpxData)
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

                if (trkptElement.Element(ns + "time") is { } timeElement)
                {
                    trkpt.Time = DateTimeOffset.Parse(timeElement.Value, CultureInfo.InvariantCulture);
                }

                if (trkptElement.Element(ns + "ele") is { } eleElement)
                {
                    trkpt.Ele = double.Parse(eleElement.Value, CultureInfo.InvariantCulture);
                }

                trkpList.Add(trkpt);
            }
        }
        return new GpxTrack(trkpList);
    }

    public async Task<bool> TryApplyGpxTrackToFile(GpxTrack gpxTrack, IBitmapFileInfo file)
    {
        if (await metadataService.GetMetadataAsync(file, MetadataProperties.DateTaken) is { } dateTaken
            && gpxTrack.FindTrackPointForTimestamp(dateTaken) is { } gpxTrackPoint)
        {
            var geoTagFromGpx = new GeoTag()
            {
                Latitude = gpxTrackPoint.Latitude,
                Longitude = gpxTrackPoint.Longitude,
                Altitude = gpxTrackPoint.Ele,
            };
            var geoTagFromFile = await metadataService.GetMetadataAsync(file, MetadataProperties.GeoTag);
            if (geoTagFromGpx != geoTagFromFile)
            {
                await metadataService.WriteMetadataAsync(file, MetadataProperties.GeoTag, geoTagFromGpx);
                return true;
            }
        }
        return false;
    }
}
