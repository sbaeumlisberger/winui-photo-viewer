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
        var rotatePhotoService = new RotateBitmapService(metadataService);
        var imageLoaderService = new ImageLoaderService(new GifImageLoaderService());
        var displayRequestService = new DisplayRequestService();
        return new FlipViewPageModel(
            session,
            messenger,
            () => new MediaFlipViewModel( messenger, dialogService, mediaFilesLoaderService, 
                (mediaItem) => new BitmapFlipViewItemModel(mediaItem, messenger, imageLoaderService)),
            () => new DetailsBarModel(metadataService),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(session, messenger, dialogService, 
                mediaFilesLoaderService, rotatePhotoService, flipViewPageModel),
            displayRequestService);
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
