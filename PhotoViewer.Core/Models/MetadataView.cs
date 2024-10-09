using MetadataAPI;

namespace PhotoViewer.Core.Models;
public class MetadataView
{
    public IDictionary<string, object?> Source { get; }

    public MetadataView(IDictionary<string, object?> source)
    {
        Source = source;
    }

    public T Get<T>(IReadonlyMetadataProperty<T> propertyDefinition)
    {
        return (T)Source[propertyDefinition.Identifier]!;
    }
}