using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.Windows.Globalization;
using PhotoViewer.Core.Models;
using System.Globalization;
using System.Text.Json;
using System.Web;

namespace PhotoViewer.Core.Services
{
    /// <summary>A service to geocode and reverse-geocode location data.</summary>
    public interface ILocationService
    {
        /// <summary>Returns the location of the specified geographic point.</summary>
        Task<Location?> FindLocationAsync(GeoPoint geoPoint);

        /// <summary>Returns a list of locations found for the given query.</summary>
        Task<IReadOnlyList<Location>> FindLocationsAsync(string query, uint maxResults = 5);

        /// <summary>Returns the nearest address of the specified geographic point. If no address is found, null is returned.</summary>
        Task<Address?> FindAddressAsync(GeoPoint geoPoint);

        /// <summary>Returns the geographic point of the specified address. If no geographic point is found, null is returned.</summary>
        Task<GeoPoint?> FindGeoPointAsync(Address address);
    }

    public class LocationService : ILocationService
    {
        private static string Language => ApplicationLanguages.Languages[0].ToString();

        public async Task<Location?> FindLocationAsync(GeoPoint geoPoint)
        {
            var uri = new Uri("https://nominatim.openstreetmap.org/reverse" + ToQueryString(
                ("lat", geoPoint.Latitude.ToString(CultureInfo.InvariantCulture)),
                ("lon", geoPoint.Longitude.ToString(CultureInfo.InvariantCulture)),
                ("format", "json"),
                ("accept-language", Language)
            ));
            Log.Debug($"Sending request to {uri}");
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UniversePhotos");
            var response = await httpClient.GetStringAsync(uri).ConfigureAwait(false);

            return ParseLocation(JsonDocument.Parse(response).RootElement);
        }

        public async Task<IReadOnlyList<Location>> FindLocationsAsync(string query, uint maxResults = 5)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query == string.Empty)
            {
                return Array.Empty<Location>();
            }

            var uri = new Uri("https://nominatim.openstreetmap.org/search" + ToQueryString(
                ("q", query),
                ("limit", maxResults.ToString()),
                ("addressdetails", "1"),
                ("format", "json"),
                ("accept-language", Language)
            ));

            Log.Debug($"Sending request to {uri}");
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UniversePhotos");
            var response = await httpClient.GetStringAsync(uri).ConfigureAwait(false);

            return ParseLocations(JsonDocument.Parse(response).RootElement);
        }

        public async Task<Address?> FindAddressAsync(GeoPoint geoPoint)
        {
            var location = await FindLocationAsync(geoPoint).ConfigureAwait(false);
            return location?.Address;
        }

        public async Task<GeoPoint?> FindGeoPointAsync(Address address)
        {
            var locations = await FindLocationsAsync(address.ToString(), 1).ConfigureAwait(false);
            return locations.FirstOrDefault()?.GeoPoint;
        }

        private List<Location> ParseLocations(JsonElement json)
        {
            var locations = new List<Location>();

            foreach (var entry in json.EnumerateArray())
            {
                locations.Add(ParseLocation(entry));
            }

            return locations;
        }

        private static Location ParseLocation(JsonElement entry)
        {
            var addressProperty = entry.GetProperty("address");

            string road = GetStringPropertyOrEmpty(addressProperty, "road");
            string houseNumber = GetStringPropertyOrEmpty(addressProperty, "house_number");
            string postcode = GetStringPropertyOrEmpty(addressProperty, "postcode");
            string city = GetStringPropertyOrEmpty(addressProperty, "town");
            string state = GetStringPropertyOrEmpty(addressProperty, "state");
            string country = GetStringPropertyOrEmpty(addressProperty, "country");

            var address = new Address()
            {
                Street = StringUtils.JoinNonEmpty(" ", road, houseNumber),
                City = StringUtils.JoinNonEmpty(" ", postcode, city),
                Region = state,
                Country = country,
            };

            if (address.ToString() == string.Empty)
            {
                address = null;
            }

            double latitude = double.Parse(entry.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
            double longitude = double.Parse(entry.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
            var geoPoint = new GeoPoint(latitude, longitude);

            return new Location(address, geoPoint);
        }

        private static string GetStringPropertyOrEmpty(JsonElement jsonElement, string propertyName)
        {
            if (jsonElement.TryGetProperty(propertyName, out var property))
            {
                return property.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private static string ToQueryString(params (string Key, string Value)[] parameters)
        {
            return "?" + string.Join("&", parameters.Select(parameter => string.Format(
                "{0}={1}",
                HttpUtility.UrlEncode(parameter.Key),
                HttpUtility.UrlEncode(parameter.Value)
            )));
        }
    }

}
