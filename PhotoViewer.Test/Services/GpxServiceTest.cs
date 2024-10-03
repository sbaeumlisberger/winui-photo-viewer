using MetadataAPI;
using MetadataAPI.Data;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using Xunit;

namespace PhotoViewer.Test.Services;

public class GpxServiceTest
{
    private const string GpxExample = """
        <?xml version="1.0" encoding="UTF-8" standalone="no" ?>
        <gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1" creator="Wikipedia"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xsi:schemaLocation="http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd">
         <!-- Kommentare sehen so aus -->
         <metadata>
          <name>Dateiname</name>
          <desc>Validiertes GPX-Beispiel ohne Sonderzeichen</desc>
          <author>
           <name>Autorenname</name>
          </author>
         </metadata>
         <wpt lat="52.518611" lon="13.376111">
          <ele>35.0</ele>
          <time>2011-12-31T23:59:59Z</time>
          <name>Reichstag (Berlin)</name>
          <sym>City</sym>
         </wpt>
         <wpt lat="48.208031" lon="16.358128">
          <ele>179</ele>
          <time>2011-12-31T23:59:59Z</time>
          <name>Parlament (Wien)</name>
          <sym>City</sym>
         </wpt>
         <wpt lat="46.9466" lon="7.44412">
          <time>2011-12-31T23:59:59Z</time>
          <name>Bundeshaus (Bern)</name>
          <sym>City</sym>
         </wpt>
         <rte>
          <name>Routenname</name>
          <desc>Routenbeschreibung</desc>
          <rtept lat="52.0" lon="13.5">
           <ele>33.0</ele>
           <time>2011-12-13T23:59:59Z</time>
           <name>rtept 1</name>
          </rtept>
          <rtept lat="49" lon="12">
           <name>rtept 2</name>
          </rtept>
          <rtept lat="47.0" lon="7.5">
          </rtept>
         </rte>
         <trk>
          <name>Trackname1</name>
          <desc>Trackbeschreibung</desc>
          <trkseg>
           <trkpt lat="52.520000" lon="13.380000">
            <ele>36.0</ele>
            <time>2011-01-13T01:01:01Z</time>
           </trkpt>
           <trkpt lat="48.200000" lon="16.260000">
            <ele>180</ele>
            <time>2011-01-14T01:59:01Z</time>
           </trkpt>
           <trkpt lat="46.95" lon="7.4">
            <ele>987.654</ele>
            <time>2011-01-15T23:59:01Z</time>
           </trkpt>
          </trkseg>
         </trk>
         <trk>
          <name>Trackname2</name>
          <trkseg>
           <trkpt lat="47.2" lon="7.41">
            <time>2011-01-16T23:59:01Z</time>
           </trkpt>
           <trkpt lat="52.53" lon="13.0">
           </trkpt>
          </trkseg>
         </trk>
        </gpx>
        """;

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly GpxService gpxService;

    public GpxServiceTest()
    {
        gpxService = new GpxService(metadataService);
    }

    [Fact]
    public void ParseGpxTrack()
    {
        var gpxTrack = gpxService.ParseGpxTrack(GpxExample);

        Assert.Equal(3, gpxTrack.Points.Count);

        Assert.Equal(52.52, gpxTrack.Points[0].Latitude);
        Assert.Equal(13.38, gpxTrack.Points[0].Longitude);
        Assert.Equal(new DateTimeOffset(2011, 1, 13, 1, 1, 1, TimeSpan.Zero), gpxTrack.Points[0].Time);
    }

    [Fact]
    public async Task AppliesGpxTrackToFile()
    {
        var dateTaken = DateTime.Now;
        var gpxTrackPoint = new GpxTrackPoint()
        {
            Latitude = 40.124848,
            Longitude = -36.128498,
            Time = dateTaken,
        };
        var gpxTrack = new GpxTrack(new[] { gpxTrackPoint });
        var file = Substitute.For<IBitmapFileInfo>();
        metadataService.GetMetadataAsync(file, MetadataProperties.DateTaken).Returns(dateTaken);
        metadataService.GetMetadataAsync(file, MetadataProperties.GeoTag).Returns((GeoTag?)null);

        bool applied = await gpxService.TryApplyGpxTrackToFile(gpxTrack, file);

        Assert.True(applied);
        await metadataService.Received().WriteMetadataAsync(file, MetadataProperties.GeoTag,
            Arg.Is<GeoTag>(geoTag => IsGeoTagEqualsGpxTrackPoint(geoTag, gpxTrackPoint)));
    }

    [Fact]
    public async Task DoesNotApplyGpxTrackToFileWihtoutDateTaken()
    {
        var gpxTrack = new GpxTrack(new List<GpxTrackPoint>());
        var file = Substitute.For<IBitmapFileInfo>();
        metadataService.GetMetadataAsync(file, MetadataProperties.DateTaken).Returns((DateTime?)null);

        bool applied = await gpxService.TryApplyGpxTrackToFile(gpxTrack, file);

        Assert.False(applied);
    }

    private bool IsGeoTagEqualsGpxTrackPoint(GeoTag geoTag, GpxTrackPoint gpxTrackPoint)
    {
        return geoTag.Latitude == gpxTrackPoint.Latitude
            && geoTag.Longitude == gpxTrackPoint.Longitude;
    }

}
