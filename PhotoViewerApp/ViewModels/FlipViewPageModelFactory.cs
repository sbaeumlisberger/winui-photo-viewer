using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.ViewModels
{
    public class FlipViewPageModelFactory
    {

        public static FlipViewPageModel Create()
        {
            var loadMediaItemsService = new LoadMediaItemsService();

            return new FlipViewPageModel(
                loadMediaItemsService,
                () => new DetailsBarModel(),
                (selectPreviousCommand, selectNextCommand) => new FlipViewPageCommandBarModel(loadMediaItemsService, selectPreviousCommand, selectNextCommand),
                (mediaItem) => new MediaFlipViewItemModel(mediaItem));
        }

    }
}
