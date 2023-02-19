using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using System.Text.Encodings.Web;
using Windows.UI.WebUI;
using System;
using Microsoft.Web.WebView2.Core;
using PhotoViewer.App.Utils.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Devices.Geolocation;
using MetadataAPI.Data;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Net;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Tocronx.SimpleAsync;
using Windows.Services.Maps;
using Windows.ApplicationModel.DataTransfer;
using System.Security.Cryptography;
using PhotoViewer.Core;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(EditLocationDialogModel))]
public sealed partial class EditLocationDialog : ContentDialog, IMVVMControl<EditLocationDialogModel>
{
    private EditLocationDialogModel ViewModel => (EditLocationDialogModel)DataContext;

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

    private readonly SequentialTaskRunner updateSuggestionsRunner = new SequentialTaskRunner();

    public EditLocationDialog()
    {
        this.InitializeComponentMVVM();
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
            await ShowLocationOnMapAsync(ViewModel.Location);
        }
    }

    private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (pivot.SelectedIndex == 0 && e.RemovedItems.Count == 1)
        {
            await ShowLocationOnMapAsync(ViewModel.Location);
        }
    }

    private async void MapWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var data = JsonSerializer.Deserialize<JsonNode>(args.WebMessageAsJson)!;
        string eventType = data["event"]!.GetValue<string>();
        if (eventType == "mapReady")
        {
            await ShowLocationOnMapAsync(ViewModel.Location);
        }
        else if (eventType == "mapClick")
        {
            double latitude = data["location"]!["latitude"]!.GetValue<double>();
            double longitude = data["location"]!["longitude"]!.GetValue<double>();
            ViewModel.OnMapClicked(latitude, longitude);
        }
    }

    private async Task ShowLocationOnMapAsync(Location? location)
    {
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

        var script = $$"""
            map.entities.clear();
            var mapLocation = new Microsoft.Maps.Location({{latitude.ToInvariantString()}}, {{longitude.ToInvariantString()}});
            var pushpin = new Microsoft.Maps.Pushpin(mapLocation, { title: '{{title}}'});
            map.entities.push(pushpin);
            map.setView({ center: mapLocation, zoom: 10 });
            """;

        await mapWebView.ExecuteScriptAsync(script);
    }

    private string FormatGeopoint(Geopoint? geopoint)
    {
        return geopoint != null ? geopoint.ToDecimalString() : "";
    }

    private void LocationSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        ViewModel.Location = (Location)args.SelectedItem;
        locationSearchBox.Text = string.Empty;
    }

    private void LocationSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        updateSuggestionsRunner.EnqueueIfEmpty(async () =>
        {
            locationSearchBox.ItemsSource = await ViewModel.FindLocationsAsync(sender.Text);
        });
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
