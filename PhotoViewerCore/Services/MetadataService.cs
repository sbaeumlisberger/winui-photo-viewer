using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils.Logging;
using System.Runtime.CompilerServices;
using WIC;
using Windows.Storage;

namespace PhotoViewerApp.Services;

public interface IMetadataService
{
    Task<MetadataView> GetMetadataAsync(IBitmapFileInfo bitmap);

    Task<T> GetMetadataAsync<T>(IBitmapFileInfo bitmap, IReadonlyMetadataProperty<T> propertyDefinition);

    Task WriteMetadataAsync(IBitmapFileInfo bitmap, MetadataPropertySet propertySet);

    Task WriteMetadataAsync<T>(IBitmapFileInfo bitmap, IMetadataProperty<T> property, T value);
}

internal class MetadataService : IMetadataService
{
    private static readonly IReadOnlyList<IReadonlyMetadataProperty> AllMetadataProperties = new IReadonlyMetadataProperty[]
    {
            MetadataProperties.Address,
            MetadataProperties.Author,
            MetadataProperties.CameraManufacturer,
            MetadataProperties.CameraModel,
            MetadataProperties.Copyright,
            MetadataProperties.DateTaken,
            MetadataProperties.ExposureTime,
            MetadataProperties.FNumber,
            MetadataProperties.FocalLengthInFilm,
            MetadataProperties.FocalLength,
            MetadataProperties.GeoTag,
            MetadataProperties.ISOSpeed,
            MetadataProperties.Keywords,
            MetadataProperties.Orientation,
            MetadataProperties.People,
            MetadataProperties.Rating,
            MetadataProperties.Title
    };

    private static readonly ConditionalWeakTable<IBitmapFileInfo, AsyncCache<MetadataView>> cacheTable = new();

    public Task<MetadataView> GetMetadataAsync(IBitmapFileInfo bitmap)
    {
        if (!bitmap.IsMetadataSupported)
        {
            throw new NotSupportedException("The file format does not support any metadata.");
        }
        var cache = cacheTable.GetOrCreateValue(bitmap);
        return cache.GetOrCreateValueAsync(() => ReadMetadataAsync(bitmap.File));
    }

    public async Task<T> GetMetadataAsync<T>(IBitmapFileInfo bitmap, IReadonlyMetadataProperty<T> propertyDefinition)
    {
        var metadata = await GetMetadataAsync(bitmap).ConfigureAwait(false);
        return metadata.Get(propertyDefinition);
    }

    public async Task WriteMetadataAsync(IBitmapFileInfo bitmap, MetadataPropertySet propertySet)
    {
        Log.Info($"Write metadata to file {bitmap.File.Name}: {string.Join(", ", propertySet.Select(entry => $"{entry.Property.Identifier} = {entry.Value}"))}");

        using (var fileStream = await bitmap.File.OpenAsync(FileAccessMode.ReadWrite).AsTask().ConfigureAwait(false))
        {
            var metadataEncoder = new MetadataEncoder(fileStream.AsStream());
            metadataEncoder.AutoResizeLargeThumbnails = true;
            metadataEncoder.SetProperties(propertySet);
            await metadataEncoder.EncodeAsync().ConfigureAwait(false);
        }
        UpdateMetadataCache(bitmap, propertySet);
    }

    public async Task WriteMetadataAsync<T>(IBitmapFileInfo bitmap, IMetadataProperty<T> property, T value)
    {
        var metadataPropertySet = new MetadataPropertySet();
        metadataPropertySet.Add(property, value);
        await WriteMetadataAsync(bitmap, metadataPropertySet);
    }

    private async Task<MetadataView> ReadMetadataAsync(IStorageFile file)
    {
        Log.Info($"Read metadata for file {file.Name}");

        using (var fileStream = await file.OpenReadAsync().AsTask().ConfigureAwait(false))
        {
            var wic = new WICImagingFactory();

            var decoder = wic.CreateDecoderFromStream(fileStream.AsStream(), WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

            var frame = decoder.GetFrame(0);

            WICSize size = frame.GetSize();

            //sizeInPixels = new Size(size.Width, size.Height);

            var metadataReader = new MetadataReader(frame.GetMetadataQueryReader(), decoder.GetDecoderInfo());

            var metadata = AllMetadataProperties.ToDictionary(
                metadataProperty => metadataProperty.Identifier,
                metadataProperty => metadataReader.GetProperty(metadataProperty));

            return new MetadataView(metadata);
        }
    }

    private void UpdateMetadataCache(IBitmapFileInfo bitmap, MetadataPropertySet metadataPropertySet)
    {
        if (cacheTable.TryGetValue(bitmap, out var cache) && cache.TryGetValue(out var metadataView))
        {
            foreach (var (property, value) in metadataPropertySet)
            {
                metadataView.Source[property.Identifier] = value;
            }
        }
    }

}


