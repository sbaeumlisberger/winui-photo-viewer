using PhotoVieweApp.Services;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.ViewModels;

public class PageModelFactory
{

    public static FlipViewPageModel CreateFlipViewPageModel(IDialogService dialogService)
    {
        var session = Session.Instance;
        var messenger = Messenger.GlobalInstance;
        var mediaFilesLoaderService = new MediaFilesLoaderService();
        var metadataService = new MetadataService();
        var rotatePhotoService = new RotatePhotoService(metadataService);
        var imageLoaderService = new ImageLoaderService();
        return new FlipViewPageModel(
            session,
            () => new MediaFlipViewModel(messenger, dialogService, mediaFilesLoaderService, (mediaItem) => new BitmapFlipViewItemModel(mediaItem, messenger, imageLoaderService)),
            () => new DetailsBarModel(metadataService),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(session, messenger, dialogService, mediaFilesLoaderService, rotatePhotoService, flipViewPageModel));
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


}
