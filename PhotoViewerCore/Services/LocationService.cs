using PhotoViewerCore.Models;
using PhotoViewerCore.Utils;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Web;
using Windows.Devices.Geolocation;
using Windows.Globalization;
using Windows.Services.Maps;

namespace PhotoViewerCore.Services
{
    /// <summary>A helper class to geocode and reverse-geocode location data via the Bing Maps REST Services.</summary>
    /// <remarks>
    /// This class is using the <see cref="MapService.ServiceToken"/> property to authenticate the REST requests.
    /// </remarks>
    public class LocationService
    {
        private string Culture => ApplicationLanguages.Languages.First().Substring(0, 2);

        /// <summary>Returns the location of the specified position.</summary>
        /// <exception cref="WebException"/>
        public async Task<Location?> FindLocationAsync(Geopoint geopoint)
        {
            if (geopoint is null)
            {
                throw new ArgumentNullException(nameof(geopoint));
            }

            string point = geopoint.Position.Latitude + "," + geopoint.Position.Longitude;
            var uri = new Uri("http://dev.virtualearth.net/REST/v1/Locations/" + point + ToQueryString(
               ("c", Culture),
               ("key", MapService.ServiceToken)
            ));
            var webClient = new WebClient();
            var responseStream = await webClient.OpenReadTaskAsync(uri).ConfigureAwait(false);
            var response = await new StreamReader(responseStream).ReadToEndAsync().ConfigureAwait(false);
            return (await ParseLocationsAsync(response).ConfigureAwait(false)).FirstOrDefault();
        }

        /// <summary>Retruns a list of locations found for the given query.</summary>
        /// <exception cref="WebException"/>
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

            var uri = new Uri("http://dev.virtualearth.net/REST/v1/Locations" + ToQueryString(
                ("query", query),
                ("c", Culture),
                ("maxResults", maxResults.ToString()),
                ("key", MapService.ServiceToken)
            ));
            var webClient = new WebClient();
            var responseStream = await webClient.OpenReadTaskAsync(uri).ConfigureAwait(false);
            var response = await new StreamReader(responseStream).ReadToEndAsync().ConfigureAwait(false);
            return await ParseLocationsAsync(response).ConfigureAwait(false);
        }

        /// <summary>Returns the location of the specified query. If no location is found, null is returned.</summary>
        /// <exception cref="WebException"/>
        public async Task<Location?> FindLocationAsync(string query)
        {
            return (await FindLocationsAsync(query, 1).ConfigureAwait(false)).FirstOrDefault();
        }

        /// <summary>Returns the nearest address of the specified position. If no address is found, null is returned.</summary>
        /// <exception cref="WebException"/>
        public async Task<Address?> FindAddressAsync(Geopoint position)
        {
            return (await FindLocationAsync(position).ConfigureAwait(false))?.Address;
        }

        /// <summary>Returns the position of the specified address. If no position is found, null is returned.</summary>
        /// <exception cref="WebException"/>
        public async Task<Geopoint?> FindGeopointAsync(Address address)
        {
            return (await FindLocationAsync(address.ToString()).ConfigureAwait(false))?.Point;
        }

        /// <summary>Returns the position of the specified address. If no position is found, null is returned.</summary>
        /// <exception cref="WebException"/>
        public async Task<Geopoint?> FindGeopointAsync(string address)
        {
            return (await FindLocationAsync(address).ConfigureAwait(false))?.Point;
        }

        private static async Task<List<Location>> ParseLocationsAsync(string response)
        {
            var locations = new List<Location>();

            var json = JsonDocument.Parse(response);
            var resources = json.RootElement.GetProperty("resourceSets").EnumerateArray().First().GetProperty("resources");

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

            string? addressLine = GetStringProperty(addressProperty, "addressLine");
            string? postalCode = GetStringProperty(addressProperty, "postalCode");
            string? locality = GetStringProperty(addressProperty, "locality");
            string? adminDistrict = GetStringProperty(addressProperty, "adminDistrict");
            string? countryRegion = GetStringProperty(addressProperty, "countryRegion");

            var address = new Address()
            {
                Street = addressLine ?? "",
                City = StringUtils.JoinNonEmpty(" ", postalCode, locality),
                Region = adminDistrict ?? "",
                Country = countryRegion ?? "",
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

        private static async Task UpdateElevationDataAsync(List<Location> locations)
        {
            double[] elevations = await FetchElevationDataAsync(locations.Select(l => l.Point)).ConfigureAwait(false);

            for (int i = 0; i < locations.Count; i++)
            {
                var updatedGeoPoint = new Geopoint
                (
                    new BasicGeoposition()
                    {
                        Latitude = locations[i].Point.Position.Latitude,
                        Longitude = locations[i].Point.Position.Longitude,
                        Altitude = elevations[i]
                    },
                    AltitudeReferenceSystem.Ellipsoid
                );
                locations[i] = new Location(locations[i].Address, updatedGeoPoint);
            }
        }

        private static async Task<double[]> FetchElevationDataAsync(IEnumerable<Geopoint> points)
        {
            var locations = new List<Location>();
            var uri = new Uri("http://dev.virtualearth.net/REST/v1/Elevation/List" + ToQueryString(
                ("points", string.Join(",", points.Select(point =>
                    point.Position.Latitude.ToString(CultureInfo.InvariantCulture)
                    + "," + point.Position.Longitude.ToString(CultureInfo.InvariantCulture)))),
                ("heights", "ellipsoid"),
                ("key", MapService.ServiceToken)
            ));
            var webClient = new WebClient();
            var response = await webClient.OpenReadTaskAsync(uri);
            var json = JsonDocument.Parse(response);
            var resources = json.RootElement.GetProperty("resourceSets").EnumerateArray().First().GetProperty("resources");
            return resources.EnumerateArray().First().GetProperty("elevations").EnumerateArray().Select(a => a.GetDouble()).ToArray();
        }

        private static string ToQueryString(params (string Key, string Value)[] parameters)
        {
            var array = parameters.Select(parameter => string.Format(
                "{0}={1}",
                HttpUtility.UrlEncode(parameter.Key),
                HttpUtility.UrlEncode(parameter.Value)
            )).ToArray();
            return "?" + string.Join("&", array);
        }

        private static string? GetStringProperty(JsonElement jsonElement, string propertyName)
        {
            if (jsonElement.TryGetProperty(propertyName, out var property))
            {
                return property.GetString();
            }
            return string.Empty;
        }
    }

}
