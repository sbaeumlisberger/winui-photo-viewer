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

        /// <summary>Retruns a list of locations found for the given query.</summary>
        Task<IReadOnlyList<Location>> FindLocationsAsync(string query, uint maxResults = 5);

        /// <summary>Returns the nearest address of the specified geographic point. If no address is found, null is returned.</summary>
        Task<Address?> FindAddressAsync(GeoPoint geoPoint);

        /// <summary>Returns the geographic point of the specified address. If no geographic point is found, null is returned.</summary>
        Task<GeoPoint?> FindGeoPointAsync(Address address);
    }

    public class LocationService : ILocationService
    {
        private static string Language => ApplicationLanguages.Languages[0].ToString();

        private static string ApiKey => CompileTimeConstants.HereApiKey;

        public async Task<Location?> FindLocationAsync(GeoPoint geoPoint)
        {
            var uri = new Uri("https://revgeocode.search.hereapi.com/v1/revgeocode" + ToQueryString(
                ("at", ToCoordinatesParam(geoPoint)),
                ("lang", Language),
                ("apiKey", ApiKey)
            ));

            Log.Debug($"Sending request to {uri.ToString().Replace(ApiKey, "****")}");

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(uri).ConfigureAwait(false);
            var locations = ParseLocations(response);
            return locations.FirstOrDefault(); ;
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

            var uri = new Uri("https://geocode.search.hereapi.com/v1/geocode" + ToQueryString(
                ("q", query),
                ("limit", maxResults.ToString()),
                ("lang", Language),
                ("apiKey", ApiKey)
            ));

            Log.Debug($"Sending request to {uri.ToString().Replace(ApiKey, "****")}");

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(uri).ConfigureAwait(false);

            return ParseLocations(response);
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

        private List<Location> ParseLocations(string response)
        {
            var locations = new List<Location>();

            var json = JsonDocument.Parse(response);
            var items = json.RootElement.GetProperty("items");

            foreach (var entry in items.EnumerateArray())
            {
                locations.Add(ParseLocation(entry));
            }

            return locations;
        }

        private static Location ParseLocation(JsonElement entry)
        {
            var addressProperty = entry.GetProperty("address");

            string street = GetStringProperty(addressProperty, "street");
            string houseNumber = GetStringProperty(addressProperty, "houseNumber");
            string postalCode = GetStringProperty(addressProperty, "postalCode");
            string city = GetStringProperty(addressProperty, "city");
            string state = GetStringProperty(addressProperty, "state");
            string countryName = GetStringProperty(addressProperty, "countryName");

            var address = new Address()
            {
                Street = StringUtils.JoinNonEmpty(" ", street, houseNumber),
                City = StringUtils.JoinNonEmpty(" ", postalCode, city),
                Region = state,
                Country = countryName,
            };

            if (address.ToString() == string.Empty)
            {
                address = null;
            }

            var positionProperty = entry.GetProperty("position");

            double latitude = positionProperty.GetProperty("lat").GetDouble();
            double longitude = positionProperty.GetProperty("lng").GetDouble();
            var geoPoint = new GeoPoint(latitude, longitude);

            return new Location(address, geoPoint);
        }

        private static string ToQueryString(params (string Key, string Value)[] parameters)
        {
            return "?" + string.Join("&", parameters.Select(parameter => string.Format(
                "{0}={1}",
                HttpUtility.UrlEncode(parameter.Key),
                HttpUtility.UrlEncode(parameter.Value)
            )));
        }

        private static string GetStringProperty(JsonElement jsonElement, string propertyName)
        {
            if (jsonElement.TryGetProperty(propertyName, out var property))
            {
                return property.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private string ToCoordinatesParam(GeoPoint geopoint)
        {
            string latitude = geopoint.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitude = geopoint.Longitude.ToString(CultureInfo.InvariantCulture);
            return latitude + "," + longitude;
        }
    }

}
