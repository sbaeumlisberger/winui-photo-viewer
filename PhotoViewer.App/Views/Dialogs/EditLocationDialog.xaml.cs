using Essentials.NET;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Geolocation;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(EditLocationDialogModel))]
public sealed partial class EditLocationDialog : ContentDialog, IMVVMControl<EditLocationDialogModel>
{
    private string BingMapHtml { get; } = $$$"""
        <!DOCTYPE html>
        <html>
            <head>
                <meta charset="utf-8" />
                <script type='text/javascript' src='https://www.bing.com/api/maps/mapcontrol?callback=GetMap&key={{{AppData.MapServiceToken}}}' async defer></script>         
                <script type="text/javascript">
                   function createMap()
                   {
                       window.map = new Microsoft.Maps.Map('#map', {});
                       Microsoft.Maps.Events.addHandler(map, 'click', function (e) { handleClick('mapClick', e); });
                       window.chrome.webview.postMessage({ event: "mapReady" });
                   }
                   
                   function handleClick(id, e) {
                       map.entities.clear();
                       window.chrome.webview.postMessage({ event: "mapClick", location: e.location });
                   }
                </script>
            </head>
            <body onload="createMap();" style="margin: 0px">
                <div id="map"></div>
             </body>
        </html>
        """;

    private readonly Throttle<string> updateSuggestionsThrottle;

    private Location? locationShowedOnMap;

    public EditLocationDialog()
    {
        this.InitializeComponentMVVM();
        updateSuggestionsThrottle = new Throttle<string>(TimeSpan.FromMilliseconds(30), UpdateSuggestionsAsync);
    }

    async partial void ConnectToViewModel(EditLocationDialogModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        await mapWebView.EnsureCoreWebView2Async();
        mapWebView.NavigateToString(BingMapHtml);
        mapWebView.WebMessageReceived += MapWebView_WebMessageReceived;
    }

    partial void DisconnectFromViewModel(EditLocationDialogModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        mapWebView.WebMessageReceived -= MapWebView_WebMessageReceived;
    }

    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Location) && pivot.SelectedIndex == 0)
        {
            await ShowLocationOnMapAsync(ViewModel!.Location, centerAndZoomLocation: false);
        }
    }

    private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (pivot.SelectedIndex == 0 && e.RemovedItems.Count == 1)
        {
            var location = ViewModel!.Location;
            if (location != locationShowedOnMap)
            {
                await ShowLocationOnMapAsync(location, centerAndZoomLocation: true);
            }
        }
    }

    private async void MapWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var data = JsonSerializer.Deserialize<JsonNode>(args.WebMessageAsJson)!;
        string eventType = data["event"]!.GetValue<string>();
        if (eventType == "mapReady")
        {
            await ShowLocationOnMapAsync(ViewModel!.Location, centerAndZoomLocation: true);
        }
        else if (eventType == "mapClick")
        {
            double latitude = data["location"]!["latitude"]!.GetValue<double>();
            double longitude = data["location"]!["longitude"]!.GetValue<double>();
            ViewModel!.OnMapClicked(latitude, longitude);
        }
    }

    private async Task ShowLocationOnMapAsync(Location? location, bool centerAndZoomLocation)
    {
        locationShowedOnMap = location;

        Geopoint? geopoint = null;

        if (location?.Geopoint != null)
        {
            geopoint = location.Geopoint;
        }
        else if (location?.Address != null)
        {
            geopoint = await new LocationService().FindGeopointAsync(location.Address);
        }

        if (geopoint is null)
        {
            await mapWebView.ExecuteScriptAsync("map.entities.clear();");
            return;
        }

        double latitude = geopoint.Position.Latitude;
        double longitude = geopoint.Position.Longitude;

        string title = location?.Address?.ToString() ?? geopoint.ToDecimalString();

        string latitudeString = latitude.ToString(CultureInfo.InvariantCulture);
        string longitudeString = longitude.ToString(CultureInfo.InvariantCulture);

        var script = $$"""
            map.entities.clear();
            var mapLocation = new Microsoft.Maps.Location({{latitudeString}}, {{longitudeString}});
            var pushpin = new Microsoft.Maps.Pushpin(mapLocation, { title: '{{title}}'});
            map.entities.push(pushpin);
            """;

        if (centerAndZoomLocation)
        {
            script += "\n";
            script += "map.setView({ center: mapLocation, zoom: 10 });";
        }

        await mapWebView.ExecuteScriptAsync(script);
    }

    private string FormatGeopoint(Geopoint? geopoint)
    {
        return geopoint != null ? geopoint.ToDecimalString() : "";
    }

    private void LocationSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is Location location)
        {
            _ = ShowLocationOnMapAsync(location, centerAndZoomLocation: true);
            ViewModel!.Location = location;
            locationSearchBox.Text = string.Empty;
        }        
    }

    private void LocationSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        updateSuggestionsThrottle.Invoke(sender.Text);
    }

    private async Task UpdateSuggestionsAsync(string query) 
    {
        locationSearchBox.ItemsSource = await ViewModel!.FindLocationsAsync(query);
    }

    private async void LatitudeTextBox_Paste(object sender, TextControlPasteEventArgs e)
    {
        if (string.IsNullOrEmpty(latitudeTextBox.Text))
        {
            e.Handled = true;

            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Contains(StandardDataFormats.Text))
            {
                try
                {
                    var text = await dataPackage.GetTextAsync();

                    if (text.Contains(","))
                    {
                        string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
                        latitudeTextBox.Text = parts[0];
                        longitudeTextBox.Text = parts[1];
                    }
                    else
                    {
                        latitudeTextBox.Text = text;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Paste coordinates failed", ex);
                }
            }
        }
    }
}
