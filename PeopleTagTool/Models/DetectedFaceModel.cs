namespace PeopleTagTool.Models;

internal record class DetectedFaceModel
{
    public uint X { get; set; }
    public uint Y { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
}
