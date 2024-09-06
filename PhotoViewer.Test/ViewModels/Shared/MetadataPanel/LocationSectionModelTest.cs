using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataEditModule.ViewModel;
using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using Windows.Devices.Geolocation;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class LocationSectionModelTest
{
    private static readonly Address Address1 = new Address()
    {
        Street = "TestStreet 1",
        City = "TestCity",
        Region = "TestRegion",
        Country = "TestCountry"
    };

    private static readonly Geopoint GeoPoint1 = new Geopoint(new BasicGeoposition()
    {
        Latitude = 40.124848,
        Longitude = -36.128498,
        Altitude = 358
    });

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly ILocationService locationService = Substitute.For<ILocationService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IViewModelFactory viewModelFactory = Substitute.For<IViewModelFactory>();

    private readonly IGpxService gpxService = Substitute.For<IGpxService>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly LocationSectionModel locationSectionModel;

    public LocationSectionModelTest()
    {
        locationSectionModel = new LocationSectionModel(metadataService, locationService, dialogService, viewModelFactory, gpxService, messenger, backgroundTaskService);
    }

    [Fact]
    public void ShowsPlaceholderAndCanNotShowLocationOnMap_WhenFileHasNoLocationInformation()
    {
        var metadata = new[] { CreateMetadataView(null, null) };

        locationSectionModel.UpdateFilesChanged(null!, metadata);

        Assert.Empty(locationSectionModel.DisplayText);
        Assert.Equal(Strings.MetadataPanel_LocationPlaceholder, locationSectionModel.PlaceholderText);
        Assert.False(locationSectionModel.ShowLocationOnMapCommand.CanExecute(null));
    }

    [Fact]
    public void ShowFullLocationAndCanShowLOcationOnMap_WhenFileHasAddressAndGeoTag()
    {
        var metadata = new[] { CreateMetadataView(Address1, GeoPoint1) };

        locationSectionModel.UpdateFilesChanged(null!, metadata);

        string expectedDiplayText = "TestStreet 1 TestCity TestRegion TestCountry (40.124848, -36.128498)";
        Assert.Equal(expectedDiplayText, locationSectionModel.DisplayText);
        Assert.True(locationSectionModel.ShowLocationOnMapCommand.CanExecute(null));
    }

    [Fact]
    public void ShowsAddressAndCanNotShowLocationOnMap_WhenFileHasOnlyAddress()
    {
        var metadata = new[] { CreateMetadataView(Address1, null) };

        locationSectionModel.UpdateFilesChanged(null!, metadata);

        string expectedDiplayText = "TestStreet 1 TestCity TestRegion TestCountry";
        Assert.Equal(expectedDiplayText, locationSectionModel.DisplayText);
        Assert.False(locationSectionModel.ShowLocationOnMapCommand.CanExecute(null));
    }

    [Fact]
    public void ShowsCoordinatesAndCanShowLocationOnMap_WhenFileHasOnlyGeoTag()
    {
        var metadata = new[] { CreateMetadataView(null, GeoPoint1) };

        locationSectionModel.UpdateFilesChanged(null!, metadata);

        string expectedDiplayText = "40.124848, -36.128498";
        Assert.Equal(expectedDiplayText, locationSectionModel.DisplayText);
        Assert.True(locationSectionModel.ShowLocationOnMapCommand.CanExecute(null));
    }

    [Fact]
    public void CompletesAddress_WhenFileHasOnlyGeoTag()
    {
        var metadata = new[] { CreateMetadataView(null, GeoPoint1) };
        locationService.FindAddressAsync(Arg.Is<Geopoint>(arg => arg.Position == GeoPoint1.Position)).Returns(Address1);

        locationSectionModel.UpdateFilesChanged(null!, metadata);

        string expectedDiplayText = "TestStreet 1 TestCity TestRegion TestCountry (40.124848, -36.128498)";
        Assert.Equal(expectedDiplayText, locationSectionModel.DisplayText);
        Assert.True(locationSectionModel.ShowLocationOnMapCommand.CanExecute(null));
    }

    [Fact]
    public void CompletesGeopoint_WhenFileHasOnlyAddress()
    {
        var metadata = new[] { CreateMetadataView(Address1, null) };
        locationService.FindGeopointAsync(Address1).Returns(GeoPoint1);

        locationSectionModel.UpdateFilesChanged(null!, metadata);

        string expectedDiplayText = "TestStreet 1 TestCity TestRegion TestCountry (40.124848, -36.128498)";
        Assert.Equal(expectedDiplayText, locationSectionModel.DisplayText);
        Assert.True(locationSectionModel.ShowLocationOnMapCommand.CanExecute(null));
    }

    private MetadataView CreateMetadataView(Address? address, Geopoint? geoTag)
    {
        return new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.Address.Identifier, address?.ToAddressTag() },
            { MetadataProperties.GeoTag.Identifier, geoTag?.ToGeoTag() }
        });
    }
}
