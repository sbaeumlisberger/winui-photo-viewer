namespace PhotoViewer.Core.Models;

public class GpxTrackPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Ele { get; set; }
    public DateTimeOffset? Time { get; set; }
}
