using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
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
        var settings = ApplicationSettingsProvider.GetSettings();
        return new FlipViewPageModel(
            session,
            messenger,
            () => new MediaFlipViewModel(messenger, dialogService, mediaFilesLoaderService,
                (mediaItem) => new BitmapFlipViewItemModel(mediaItem, messenger, imageLoaderService), settings),
            () => new DetailsBarModel(metadataService),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(session, messenger, dialogService,
                mediaFilesLoaderService, rotatePhotoService, flipViewPageModel, deleteMediaService, settings),
            displayRequestService,
            showTagPeopleOnPhotoButton => new MetadataPanelModel(messenger, showTagPeopleOnPhotoButton));
    }

    public static OverviewPageModel CreateOverviewPageModel(IDialogService dialogService)
    {
        var session = Session.Instance;
        var messenger = Messenger.GlobalInstance;
        return new OverviewPageModel(
            session,
            messenger,
            dialogService);
    }

    public static SettingsPageModel CreateSettingsPageModel(IDialogService dialogService)
    {
        var messenger = Messenger.GlobalInstance;
        var settings = ApplicationSettingsProvider.GetSettings();
        var settingService = new SettingsService();
        return new SettingsPageModel(messenger, settings, settingService, dialogService);
    }
}
