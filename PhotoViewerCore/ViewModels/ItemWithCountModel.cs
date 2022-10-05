namespace PhotoViewerCore.ViewModels;

public class ItemWithCountModel
{
    public string Value { get; }

    public int Count { get; }

    public int Total { get; }

    public ItemWithCountModel(string value, int count, int total)
    {
        Value = value;
        Count = count;
        Total = total;
    }
}
