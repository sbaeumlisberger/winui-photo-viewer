using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace PhotoViewerCore.ViewModels;

public partial class EditLocationDialogModel : ViewModelBase
{
    public Location? Location { get; set; }

    public EditLocationDialogDetailsTabModel Details { get; } = new EditLocationDialogDetailsTabModel();

    private readonly ILocationService locationService;

    public EditLocationDialogModel(Location? location, ILocationService locationService)
    {
        Location = location;
        this.locationService = locationService;
        Details.LocationDetailsChanged += Details_LocationDetailsChanged;
    }

    private void Details_LocationDetailsChanged(object? sender, Location location)
    {
        Location = location;
    }

    partial void OnLocationChanged()
    {
        Details.Update(Location);
    }

    public async void OnMapClicked(double latitude, double longitude)
    {
        var geopositon = new BasicGeoposition()
        {
            Latitude = latitude,
            Longitude = longitude
        };
        var geopoint = new Geopoint(geopositon, AltitudeReferenceSystem.Unspecified);
        Location = await locationService.FindLocationAsync(geopoint);
    }

    public async Task<IReadOnlyList<Location>> FindLocationsAsync(string query)
    {
        return await locationService.FindLocationsAsync(query);
    }
}
