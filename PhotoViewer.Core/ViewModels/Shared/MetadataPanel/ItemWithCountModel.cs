namespace PhotoViewer.Core.ViewModels;

public partial record class ItemWithCountModel
{
    public string Value { get; }

    public string ShortValue { get; }

    public int Count { get; }

    public int Total { get; }

    public bool ShowCount => Total > 1;

    public ItemWithCountModel(string value, int count, int total)
    {
        Value = value;
        ShortValue = value;
        Count = count;
        Total = total;
    }

    public ItemWithCountModel(string value, string shortValue, int count, int total)
    {
        Value = value;
        ShortValue = shortValue;
        Count = count;
        Total = total;
    }
}
