using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Commands;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.ViewModels;

public class PageModelFactory
{

    public static FlipViewPageModel CreateFlipViewPageModel(IDialogService dialogService)
    {
        var session = Session.Instance;
        var messenger = Messenger.GlobalInstance;
        var mediaFilesLoaderService = new MediaFilesLoaderService();
        var metadataService = new MetadataService();
        var rotatePhotoService = new RotateBitmapService(metadataService);
        var imageLoaderService = new ImageLoaderService(new GifImageLoaderService());
        var displayRequestService = new DisplayRequestService();
        var deleteMediaService = new DeleteMediaService();
        var locationService = new LocationService();
        var settings = ApplicationSettingsProvider.GetSettings();
        return new FlipViewPageModel(
            session,
            messenger,
            () => new MediaFlipViewModel(messenger, dialogService, mediaFilesLoaderService,
                (mediaItem) => new BitmapFlipViewItemModel(mediaItem, messenger, imageLoaderService), settings),
            () => new DetailsBarModel(messenger, metadataService),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(session, messenger, dialogService,
                mediaFilesLoaderService, rotatePhotoService, flipViewPageModel, deleteMediaService, settings),
            displayRequestService,
            showTagPeopleOnPhotoButton => new MetadataPanelModel(messenger, metadataService, locationService, showTagPeopleOnPhotoButton));
    }

    public static OverviewPageModel CreateOverviewPageModel(IDialogService dialogService)
    {
        var session = Session.Instance;
        var messenger = Messenger.GlobalInstance;
        var metadataService = new MetadataService();
        var locationService = new LocationService();
        var mediaFilesLoaderService = new MediaFilesLoaderService();
        var deleteMediaService = new DeleteMediaService();
        var settingService = new SettingsService();
        var settings = ApplicationSettingsProvider.GetSettings();
        var deleteFilesCommand = new DeleteFilesCommand(messenger, deleteMediaService, dialogService, settingService, settings);
        return new OverviewPageModel(
            session,
            messenger,
            dialogService,
            new OverviewPageCommandBarModel(messenger, dialogService, mediaFilesLoaderService, deleteFilesCommand, settings),
            showTagPeopleOnPhotoButton => new MetadataPanelModel(messenger, metadataService, locationService, showTagPeopleOnPhotoButton));
    }

    public static SettingsPageModel CreateSettingsPageModel(IDialogService dialogService)
    {
        var messenger = Messenger.GlobalInstance;
        var settings = ApplicationSettingsProvider.GetSettings();
        var settingService = new SettingsService();
        return new SettingsPageModel(messenger, settings, settingService, dialogService);
    }
}
