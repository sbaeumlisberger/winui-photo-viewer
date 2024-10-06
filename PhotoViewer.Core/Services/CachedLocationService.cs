using Essentials.NET;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Services;

internal class CachedLocationService : ILocationService
{
    private readonly ILocationService locationService;

    private readonly AsyncCache<GeoPoint, Location?> locationCache = new(null);
    private readonly AsyncCache<Address, GeoPoint?> geoPointCache = new(null);
    private readonly AsyncCache<(string Query, uint MaxResults), IReadOnlyList<Location>> searchResultCache = new(null);

    public CachedLocationService(ILocationService locationService)
    {
        this.locationService = locationService;
    }

    public async Task<Address?> FindAddressAsync(GeoPoint geoPoint)
    {
        var location = await FindLocationAsync(geoPoint).ConfigureAwait(false);
        return location?.Address;
    }

    public Task<GeoPoint?> FindGeoPointAsync(Address address)
    {
        return geoPointCache.GetOrCreateAsync(address, (address, _) => locationService.FindGeoPointAsync(address));
    }

    public Task<Location?> FindLocationAsync(GeoPoint geoPoint)
    {
        var geoPointWithTolerance = new GeoPoint(Math.Round(geoPoint.Latitude, 5), Math.Round(geoPoint.Longitude, 5));
        return locationCache.GetOrCreateAsync(geoPointWithTolerance, (geoPoint, _) => locationService.FindLocationAsync(geoPoint));
    }

    public Task<IReadOnlyList<Location>> FindLocationsAsync(string query, uint maxResults = 5)
    {
        return searchResultCache.GetOrCreateAsync((query, maxResults), (args, _) => locationService.FindLocationsAsync(args.Query, args.MaxResults));
    }
}
