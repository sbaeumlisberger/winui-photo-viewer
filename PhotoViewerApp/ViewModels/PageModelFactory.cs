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
        var loadMediaItemsService = new LoadMediaItemsService();
        return new FlipViewPageModel(
            session,
            () => new MediaFlipViewModel(messenger, dialogService, loadMediaItemsService, (mediaItem) => new MediaFlipViewItemModel(mediaItem, new ImageLoadService())),
            () => new DetailsBarModel(),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(session, messenger, dialogService, loadMediaItemsService, flipViewPageModel));
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
