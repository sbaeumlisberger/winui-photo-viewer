using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace PhotoViewer.Core.ViewModels;

public partial class EditLocationDialogModel : ViewModelBase
{
    public Location? Location { get; set; }

    public EditLocationDialogDetailsTabModel Details { get; }

    public bool CanSave { get; set; } = true;

    private readonly ILocationService locationService;

    private readonly Func<Location?, Task> onSave;

    public EditLocationDialogModel(Location? location, Func<Location?, Task> onSave, ILocationService locationService, IClipboardService clipboardService)
    {
        this.locationService = locationService;
        this.onSave = onSave;
        Details = new EditLocationDialogDetailsTabModel(clipboardService);
        Details.LocationChanged += Details_LocationChanged;
        Details.PropertyChanged += Details_PropertyChanged;
        Location = location;
        Details.Update(Location);
    }

    private void Details_LocationChanged(object? sender, Location? location)
    {
        Location = location;
    }

    private void Details_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Details.CanSave))
        {
            CanSave = Details.CanSave;
        }
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

    [RelayCommand]
    public void RemoveLocation() 
    {
        Location = null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task Save()
    {
        await onSave.Invoke(Location);
    }

}
