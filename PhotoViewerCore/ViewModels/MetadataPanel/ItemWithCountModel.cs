namespace PhotoViewerCore.ViewModels;

public record class ItemWithCountModel
{
    public string Value { get; }

    public int Count { get; }

    public int Total { get; }

    public bool ShowCount => Total > 1;

    public ItemWithCountModel(string value, int count, int total)
    {
        Value = value;
        Count = count;
        Total = total;
    }
}
