namespace PhotoViewer.Core.Models;

public record Address
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;

    public override string ToString()
    {
        return string.Join(" ", new[] { Street, City, Region, Country }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
