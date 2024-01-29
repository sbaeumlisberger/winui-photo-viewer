using PhotoViewer.Core.Models;
using Xunit;

namespace PhotoViewer.Test.Models;

public class GpxTrackTest
{
    private readonly List<GpxTrackPoint> trackPoints = new()
    {
        new GpxTrackPoint() { Time = new DateTimeOffset(2020, 6, 10, 14, 53, 17, TimeSpan.Zero) },
        new GpxTrackPoint() { Time = new DateTimeOffset(2020, 6, 10, 14, 53, 19, TimeSpan.Zero) },
        new GpxTrackPoint() { Time = new DateTimeOffset(2020, 6, 10, 14, 53, 23, TimeSpan.Zero) },
        new GpxTrackPoint() { Time = new DateTimeOffset(2020, 6, 10, 14, 53, 24, TimeSpan.Zero) },
        new GpxTrackPoint() { Time = new DateTimeOffset(2020, 6, 10, 14, 53, 29, TimeSpan.Zero) },
    };

    private readonly GpxTrack gpxTrack;

    public GpxTrackTest()
    {
        gpxTrack = new GpxTrack(trackPoints);
    }

    [Fact]
    public void FindTrackPointForTimestamp_ExactMatch()
    {
        var timestamp = new DateTimeOffset(2020, 6, 10, 14, 53, 19, TimeSpan.Zero);

        var trackPoint = gpxTrack.FindTrackPointForTimestamp(timestamp);

        Assert.Equal(trackPoints[1], trackPoint);
    }

    [Fact]
    public void FindTrackPointForTimestamp_NearestToEarlierPoint()
    {
        var timestamp = new DateTimeOffset(2020, 6, 10, 14, 53, 20, TimeSpan.Zero);

        var trackPoint = gpxTrack.FindTrackPointForTimestamp(timestamp);

        Assert.Equal(trackPoints[1], trackPoint);
    }

    [Fact]
    public void FindTrackPointForTimestamp_NearestToLaterPoint()
    {
        var timestamp = new DateTimeOffset(2020, 6, 10, 14, 53, 22, TimeSpan.Zero);

        var trackPoint = gpxTrack.FindTrackPointForTimestamp(timestamp);

        Assert.Equal(trackPoints[2], trackPoint);
    }


    [Fact]
    public void FindTrackPointForTimestamp_BeforeTrackStart()
    {
        var timestamp = new DateTimeOffset(2020, 6, 10, 14, 53, 0, TimeSpan.Zero);

        var trackPoint = gpxTrack.FindTrackPointForTimestamp(timestamp);

        Assert.Null(trackPoint);
    }


    [Fact]
    public void FindTrackPointForTimestamp_AfterTrackStart()
    {
        var timestamp = new DateTimeOffset(2020, 6, 10, 14, 54, 0, TimeSpan.Zero);

        var trackPoint = gpxTrack.FindTrackPointForTimestamp(timestamp);

        Assert.Null(trackPoint);
    }

    [Fact]
    public void FindTrackPointForTimestamp_TrackWithoutPoints()
    {
        var gpxTrack = new GpxTrack(new List<GpxTrackPoint>());
        var timestamp = new DateTimeOffset(2020, 6, 10, 14, 53, 19, TimeSpan.Zero);

        var trackPoint = gpxTrack.FindTrackPointForTimestamp(timestamp);

        Assert.Null(trackPoint);
    }
}
