namespace PhotoViewer.Core.Models;

internal class GpxTrack
{
    public IReadOnlyList<GpxTrackPoint> Points { get; }

    public GpxTrack(IReadOnlyList<GpxTrackPoint> points)
    {
        Points = points;
    }

    public GpxTrackPoint? FindTrackPointForTimestamp(DateTimeOffset timestamp)
    {
        var trackPointsWithTime = Points.Where(trkpt => trkpt.Time != null).ToList();

        if (!trackPointsWithTime.Any()
            || timestamp < trackPointsWithTime.First().Time
            || timestamp > trackPointsWithTime.Last().Time)
        {
            return null;
        }

        return trackPointsWithTime.MinBy(trkpt => (timestamp - trkpt.Time!.Value).Duration());
    }

}
