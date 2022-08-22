using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

//namespace PhotoViewerApp
//{
//    public partial class Ioc 
//    {
//        public ILoadMediaItemsService CreateLoadMediaItemsService()
//        {
//            return new LoadMediaItemsService();
//        }

//    }
//}

namespace PhotoViewerApp.Services
{

    public interface ILoadMediaItemsService
    {

        Task LoadMediaItems(IActivatedEventArgs activatedEventArgs);
        Task LoadMediaItems(StorageFolder storageFolder);
    }

    internal class LoadMediaItemsService : ILoadMediaItemsService
    {
        public async Task LoadMediaItems(IActivatedEventArgs activatedEventArgs)
        {
            List<IMediaItem> mediaItems;
            IMediaItem? startItem;

            if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
            {
                var files = await fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery.GetFilesAsync();
                mediaItems = new List<IMediaItem>(files.Select(file => new BitmapGraphicItem(file)));
                startItem = mediaItems.First(mediaItem => mediaItem.File.Path == fileActivatedEventArgsWithNeighboringFiles.Files.First().Path);
            }
            else if (activatedEventArgs is IFileActivatedEventArgs fileActivatedEventArgs)
            {
                mediaItems = new List<IMediaItem>(fileActivatedEventArgs.Files.Select(file => new BitmapGraphicItem((IStorageFile)file)));
                startItem = mediaItems.First();
            }
            else
            {
#if DEBUG
                var files = await KnownFolders.PicturesLibrary.GetFilesAsync();
                mediaItems = new List<IMediaItem>(files.Select(file => new BitmapGraphicItem(file)));
                startItem = mediaItems.First();
#else
            mediaItems =  new List<IMediaItem>();
            startItem = null;
#endif
            }
            Messenger.GetForCurrentThread().Publish(new MediaItemsLoadedMessage(mediaItems, startItem));
        }

        public async Task LoadMediaItems(StorageFolder storageFolder)
        {
            var files = await storageFolder.GetFilesAsync();
            var mediaItems = new List<IMediaItem>(files.Select(file => new BitmapGraphicItem(file)));
            var startItem = mediaItems.FirstOrDefault();
            Messenger.GetForCurrentThread().Publish(new MediaItemsLoadedMessage(mediaItems, startItem));

        }

    }
}
