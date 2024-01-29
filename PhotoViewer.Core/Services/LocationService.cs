using Essentials.NET;
using PhotoViewer.Core.Models;
using System.Globalization;
using System.Text.Json;
using System.Web;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace PhotoViewer.Core.Services
{
    /// <summary>A service to geocode and reverse-geocode location data.</summary>
    public interface ILocationService
    {
        /// <summary>Returns the location of the specified position.</summary>
        Task<Location?> FindLocationAsync(Geopoint geopoint);

        /// <summary>Retruns a list of locations found for the given query.</summary>
        Task<IReadOnlyList<Location>> FindLocationsAsync(string query, uint maxResults = 5);

        /// <summary>Returns the location of the specified query. If no location is found, null is returned.</summary>
        Task<Location?> FindLocationAsync(string query);

        /// <summary>Returns the nearest address of the specified position. If no address is found, null is returned.</summary>
        Task<Address?> FindAddressAsync(Geopoint position);

        /// <summary>Returns the position of the specified address. If no position is found, null is returned.</summary>
        Task<Geopoint?> FindGeopointAsync(Address address);

        /// <summary>Returns the position of the specified address. If no position is found, null is returned.</summary>
        Task<Geopoint?> FindGeopointAsync(string address);
    }

    /// <summary>A service to geocode and reverse-geocode location data via the Bing Maps REST Services.</summary>
    /// <remarks>
    /// This class is using the <see cref="MapService.ServiceToken"/> property to authenticate the REST requests.
    /// </remarks>
    public class LocationService : ILocationService
    {
        private const string BaseURL = "http://dev.virtualearth.net/REST/v1";

        private string BingCultureCode => CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        private readonly string mapServiceToken = AppData.MapServiceToken;

        public async Task<Location?> FindLocationAsync(Geopoint geopoint)
        {
            if (geopoint is null)
            {
                throw new ArgumentNullException(nameof(geopoint));
            }
            var uri = new Uri(BaseURL + "/Locations/" + ToPointString(geopoint) + ToQueryString(
                ("key", mapServiceToken),
                ("culture", BingCultureCode),
                ("verboseplacenames", "true")
            ));
            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(uri).ConfigureAwait(false);
            return (await ParseLocationsAsync(response).ConfigureAwait(false)).FirstOrDefault();
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
            var uri = new Uri(BaseURL + "/Locations" + ToQueryString(
                ("key", mapServiceToken),
                ("culture", BingCultureCode),
                ("verboseplacenames", "true"),
                ("query", query),
                ("maxResults", maxResults.ToString())
            ));
            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(uri).ConfigureAwait(false);
            return await ParseLocationsAsync(response).ConfigureAwait(false);
        }

        public async Task<Location?> FindLocationAsync(string query)
        {
            return (await FindLocationsAsync(query, 1).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<Address?> FindAddressAsync(Geopoint position)
        {
            return (await FindLocationAsync(position).ConfigureAwait(false))?.Address;
        }

        public async Task<Geopoint?> FindGeopointAsync(Address address)
        {
            return (await FindLocationAsync(address.ToString()).ConfigureAwait(false))?.Geopoint;
        }

        public async Task<Geopoint?> FindGeopointAsync(string address)
        {
            return (await FindLocationAsync(address).ConfigureAwait(false))?.Geopoint;
        }

        private async Task<List<Location>> ParseLocationsAsync(string response)
        {
            var locations = new List<Location>();

            var json = JsonDocument.Parse(response);
            var resources = json.RootElement.GetProperty("resourceSets")
                .EnumerateArray().First().GetProperty("resources");

            foreach (var entry in resources.EnumerateArray())
            {
                locations.Add(ParseLocation(entry));
            }

            if (locations.Any())
            {
                // retrieving the elevation data requires an additional request
                await UpdateElevationDataAsync(locations).ConfigureAwait(false);
            }

            return locations;
        }

        private static Location ParseLocation(JsonElement entry)
        {
            var addressProperty = entry.GetProperty("address");

            string addressLine = GetStringProperty(addressProperty, "addressLine");
            string postalCode = GetStringProperty(addressProperty, "postalCode");
            string locality = GetStringProperty(addressProperty, "locality");
            string adminDistrict = GetStringProperty(addressProperty, "adminDistrict");
            string countryRegion = GetStringProperty(addressProperty, "countryRegion");

            var address = new Address()
            {
                Street = addressLine,
                City = StringUtils.JoinNonEmpty(" ", postalCode, locality),
                Region = adminDistrict,
                Country = countryRegion,
            };

            if (address.ToString() == string.Empty)
            {
                address = null;
            }

            var coordinatesArray = entry.GetProperty("point").GetProperty("coordinates");
            var coordinatesEnumerator = coordinatesArray.EnumerateArray();
            var geoPosition = new BasicGeoposition();
            coordinatesEnumerator.MoveNext();
            geoPosition.Latitude = coordinatesEnumerator.Current.GetDouble();
            coordinatesEnumerator.MoveNext();
            geoPosition.Longitude = coordinatesEnumerator.Current.GetDouble();
            geoPosition.Altitude = 0;
            var point = new Geopoint(geoPosition, AltitudeReferenceSystem.Unspecified);

            return new Location(address, point);
        }

        private async Task UpdateElevationDataAsync(List<Location> locations)
        {
            var geopoints = locations.Select(location => location.Geopoint!).ToList();

            double[] elevations = await FetchElevationDataAsync(geopoints).ConfigureAwait(false);

            for (int i = 0; i < locations.Count; i++)
            {
                var updatedGeoPoint = new Geopoint
                (
                    new BasicGeoposition()
                    {
                        Latitude = geopoints[i].Position.Latitude,
                        Longitude = geopoints[i].Position.Longitude,
                        Altitude = elevations[i]
                    },
                    AltitudeReferenceSystem.Ellipsoid
                );
                locations[i] = new Location(locations[i].Address, updatedGeoPoint);
            }
        }

        private async Task<double[]> FetchElevationDataAsync(IList<Geopoint> geopoints)
        {
            var locations = new List<Location>();
            var uri = new Uri(BaseURL + "/Elevation/List" + ToQueryString(
                ("key", mapServiceToken),
                ("points", string.Join(",", geopoints.Select(ToPointString))),
                ("heights", "ellipsoid")
            ));
            var httpClient = new HttpClient();
            var reponseStream = await httpClient.GetStreamAsync(uri).ConfigureAwait(false);
            var json = JsonDocument.Parse(reponseStream);
            var resources = json.RootElement.GetProperty("resourceSets")
                .EnumerateArray().First().GetProperty("resources");
            return resources.EnumerateArray().First().GetProperty("elevations")
                .EnumerateArray().Select(elevation => elevation.GetDouble()).ToArray();
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

        private string ToPointString(Geopoint geopoint)
        {
            string latitude = geopoint.Position.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitude = geopoint.Position.Longitude.ToString(CultureInfo.InvariantCulture);
            return latitude + "," + longitude;
        }
    }

}
