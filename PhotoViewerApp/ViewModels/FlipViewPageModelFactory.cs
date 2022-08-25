using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.ViewModels;

public class FlipViewPageModelFactory
{

    public static FlipViewPageModel Create(IDialogService dialogService)
    {
        var messenger = Messenger.GlobalInstance;
        var loadMediaItemsService = new LoadMediaItemsService();
        return new FlipViewPageModel(
            messenger,
            dialogService,
            loadMediaItemsService,
            () => new DetailsBarModel(),
            (selectPreviousCommand, selectNextCommand) => new FlipViewPageCommandBarModel(
                messenger, dialogService, loadMediaItemsService, selectPreviousCommand, selectNextCommand),
            (mediaItem) => new MediaFlipViewItemModel(mediaItem, new ImageLoadService()));
    }

}
