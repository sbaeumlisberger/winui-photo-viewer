using Essentials.NET.Logging;
using MetadataAPI;
using PhotoViewer.Core.Models;
using System.Runtime.CompilerServices;
using WIC;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public interface IMetadataService
{
    Task<MetadataView> GetMetadataAsync(IBitmapFileInfo bitmap);

    Task<T> GetMetadataAsync<T>(IBitmapFileInfo bitmap, IReadonlyMetadataProperty<T> propertyDefinition);

    Task WriteMetadataAsync(IBitmapFileInfo bitmap, MetadataPropertySet propertySet);

    Task WriteMetadataAsync<T>(IBitmapFileInfo bitmap, IMetadataProperty<T> property, T value);

    void UpdateCache<T>(IBitmapFileInfo bitmap, IMetadataProperty<T> property, T value);

    void InvalidateCache(IBitmapFileInfo bitmap);
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

    private static readonly ConditionalWeakTable<IBitmapFileInfo, Task<MetadataView>> cacheTable = new();

    public Task<MetadataView> GetMetadataAsync(IBitmapFileInfo bitmap)
    {
        if (!bitmap.IsMetadataSupported)
        {
            throw new NotSupportedException("The file format does not support any metadata.");
        }
        return cacheTable.GetValue(bitmap, bitmap => ReadMetadataAsync(bitmap));
    }

    public async Task<T> GetMetadataAsync<T>(IBitmapFileInfo bitmap, IReadonlyMetadataProperty<T> propertyDefinition)
    {
        var metadata = await GetMetadataAsync(bitmap).ConfigureAwait(false);
        return metadata.Get(propertyDefinition);
    }

    public async Task WriteMetadataAsync(IBitmapFileInfo bitmap, MetadataPropertySet propertySet)
    {
        Log.Info($"Write metadata to file {bitmap.FileName}: {string.Join(", ", propertySet.Select(entry => $"{entry.Property.Identifier} = {entry.Value}"))}");

        using (var fileStream = await bitmap.OpenAsync(FileAccessMode.ReadWrite).ConfigureAwait(false))
        {
            var metadataEncoder = new MetadataEncoder(fileStream);
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

    public void UpdateCache<T>(IBitmapFileInfo bitmap, IMetadataProperty<T> property, T value)
    {
        if (cacheTable.TryGetValue(bitmap, out var metadataViewTask) && metadataViewTask.IsCompletedSuccessfully)
        {
            metadataViewTask.Result.Source[property.Identifier] = value;
        }
    }

    private Task<MetadataView> ReadMetadataAsync(IBitmapFileInfo file)
    {
        return Task.Run(async () =>
        {
            Log.Info($"Read metadata for file {file.DisplayName}");

            using (var fileStream = await file.OpenAsync(FileAccessMode.Read).ConfigureAwait(false))
            {
                var wic = WICImagingFactory.Create();

                var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

                var frame = decoder.GetFrame(0);

                var metadataReader = new MetadataReader(frame.GetMetadataQueryReader(), decoder.GetDecoderInfo());

                var metadata = AllMetadataProperties.ToDictionary(
                    metadataProperty => metadataProperty.Identifier,
                    metadataProperty => metadataReader.GetProperty(metadataProperty));

                return new MetadataView(metadata);
            }
        });
    }

    private void UpdateMetadataCache(IBitmapFileInfo bitmap, MetadataPropertySet metadataPropertySet)
    {
        if (cacheTable.TryGetValue(bitmap, out var metadataViewTask) && metadataViewTask.IsCompletedSuccessfully)
        {
            foreach (var (property, value) in metadataPropertySet)
            {
                metadataViewTask.Result.Source[property.Identifier] = value;
            }
        }
    }

    public void InvalidateCache(IBitmapFileInfo bitmap)
    {
        cacheTable.Remove(bitmap);
    }
}


