using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;
using System.Text.Encodings.Web;
using Windows.UI.WebUI;
using System;
using Microsoft.Web.WebView2.Core;
using PhotoViewerApp.Utils.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Devices.Geolocation;
using MetadataAPI.Data;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using System.Net;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Tocronx.SimpleAsync;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(EditLocationDialogModel))]
public sealed partial class EditLocationDialog : ContentDialog, IMVVMControl<EditLocationDialogModel>
{
    private EditLocationDialogModel ViewModel => (EditLocationDialogModel)DataContext;

    private string BingMapHtml { get; } = $$"""
        <!DOCTYPE html>
        <html>
            <head>
                <meta charset="utf-8" />
                <script type='text/javascript' src='https://www.bing.com/api/maps/mapcontrol?callback=GetMap&key={{App.MapServiceToken}}' async defer></script>         
                <script type="text/javascript">
                   function createMap()
                   {
                     window.map = new Microsoft.Maps.Map('#map', {});
                     Microsoft.Maps.Events.addHandler(map, 'click', function (e) { handleClick('mapClick', e); });
                   }
                   
                   function handleClick(id, e) {
                     map.entities.clear();
                     window.chrome.webview.postMessage(e.location);
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
        this.InitializeMVVM(OnViewModelConnected, OnViewModelDisconnected);
        Loaded += EditLocationDialog_Loaded;
    }

    private void OnViewModelConnected(EditLocationDialogModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void OnViewModelDisconnected(EditLocationDialogModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditLocationDialogModel.Location))
        {
            await ShowLocationOnMapAsync(ViewModel.Location);
        }
    }

    private async void EditLocationDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await mapWebView.EnsureCoreWebView2Async();
        mapWebView.NavigateToString(BingMapHtml);
        mapWebView.WebMessageReceived += MapWebView_WebMessageReceived;
        await ShowLocationOnMapAsync(ViewModel.Location);
    }

    private void MapWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var data = JsonSerializer.Deserialize<JsonNode>(args.WebMessageAsJson)!;
        double latitude = data["latitude"]!.GetValue<double>();
        double longitude = data["longitude"]!.GetValue<double>();
        ViewModel.OnMapClicked(latitude, longitude);
    }

    private async Task ShowLocationOnMapAsync(Location? location)
    {
        if (location?.Point is null)
        {
            return;
        }

        double latitude = location.Point!.Position.Latitude;
        double longitude = location.Point.Position.Longitude;

        var script = $$"""
            map.entities.clear();
            var mapLocation = new Microsoft.Maps.Location({{latitude.ToInvariantString()}}, {{longitude.ToInvariantString()}});
            var pushpin = new Microsoft.Maps.Pushpin(mapLocation, { title: '{{location.Address}}'});
            map.entities.push(pushpin);
            """;

        await mapWebView.ExecuteScriptAsync(script);
    }

    private string FormatGeopoint(Geopoint? geopoint)
    {
        if (geopoint is null)
        {
            return "";
        }
        return geopoint.Position.Latitude + "° " + geopoint.Position.Longitude + "°";
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

}
