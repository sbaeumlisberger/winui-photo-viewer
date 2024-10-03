using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.Windows.Globalization;
using PhotoViewer.Core.Models;
using System.Globalization;
using System.Text.Json;
using System.Web;
using Windows.Devices.Geolocation;

namespace PhotoViewer.Core.Services
{
    /// <summary>A service to geocode and reverse-geocode location data.</summary>
    public interface ILocationService
    {
        /// <summary>Returns the location of the specified position.</summary>
        Task<Location?> FindLocationAsync(Geopoint geopoint);

        /// <summary>Retruns a list of locations found for the given query.</summary>
        Task<IReadOnlyList<Location>> FindLocationsAsync(string query, uint maxResults = 5);

        /// <summary>Returns the nearest address of the specified position. If no address is found, null is returned.</summary>
        Task<Address?> FindAddressAsync(Geopoint position);

        /// <summary>Returns the position of the specified address. If no position is found, null is returned.</summary>
        Task<Geopoint?> FindGeopointAsync(Address address);
    }

    public class LocationService : ILocationService
    {
        private static string Language => ApplicationLanguages.Languages[0].ToString();

        private static string ApiKey => CompileTimeConstants.HereApiKey;

        public async Task<Location?> FindLocationAsync(Geopoint geopoint)
        {
            if (geopoint is null)
            {
                throw new ArgumentNullException(nameof(geopoint));
            }

            var uri = new Uri("https://revgeocode.search.hereapi.com/v1/revgeocode" + ToQueryString(
                ("at", ToCoordinatesParam(geopoint)),
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

        public async Task<Address?> FindAddressAsync(Geopoint position)
        {
            var location = await FindLocationAsync(position).ConfigureAwait(false);
            return location?.Address;
        }

        public async Task<Geopoint?> FindGeopointAsync(Address address)
        {
            var locations = await FindLocationsAsync(address.ToString(), 1).ConfigureAwait(false);
            return locations.FirstOrDefault()?.Geopoint;
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

            var geoPosition = new BasicGeoposition();
            geoPosition.Latitude = positionProperty.GetProperty("lat").GetDouble();
            geoPosition.Longitude = positionProperty.GetProperty("lng").GetDouble();
            var point = new Geopoint(geoPosition, AltitudeReferenceSystem.Unspecified);

            return new Location(address, point);
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

        private string ToCoordinatesParam(Geopoint geopoint)
        {
            string latitude = geopoint.Position.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitude = geopoint.Position.Longitude.ToString(CultureInfo.InvariantCulture);
            return latitude + "," + longitude;
        }
    }

}
