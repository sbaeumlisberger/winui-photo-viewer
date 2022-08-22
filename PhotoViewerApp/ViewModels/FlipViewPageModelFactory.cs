using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.ViewModels;

public class FlipViewPageModelFactory
{

    public static FlipViewPageModel Create()
    {
        var messenger = Messenger.GetForCurrentThread();
        var dialogService = DialogService.GetForCurrentWindow();
        var loadMediaItemsService = new LoadMediaItemsService();
        return new FlipViewPageModel(
            messenger,
            dialogService,
            loadMediaItemsService,
            () => new DetailsBarModel(),
            (selectPreviousCommand, selectNextCommand) => new FlipViewPageCommandBarModel(
                messenger, dialogService, loadMediaItemsService, selectPreviousCommand, selectNextCommand),
            (mediaItem) => new MediaFlipViewItemModel(mediaItem));
    }

}
