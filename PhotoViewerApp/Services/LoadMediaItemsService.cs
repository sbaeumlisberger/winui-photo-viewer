using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace PhotoViewerApp.Services;

public record class LoadMediaItemsResult(List<IMediaItem> MediaItems, IMediaItem? StartItem);

public interface ILoadMediaItemsService
{

    Task<LoadMediaItemsResult> LoadMediaItems(IActivatedEventArgs activatedEventArgs);
    Task<LoadMediaItemsResult> LoadMediaItems(StorageFolder storageFolder);
}

internal class LoadMediaItemsService : ILoadMediaItemsService
{
    public async Task<LoadMediaItemsResult> LoadMediaItems(IActivatedEventArgs activatedEventArgs)
    {
        List<IMediaItem> mediaItems;
        IMediaItem? startItem;

        if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
        {
            var files = await fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery.GetFilesAsync();
            mediaItems = ConvertFilesToMediaItems(files);
            startItem = mediaItems.First(mediaItem => mediaItem.File.Path == fileActivatedEventArgsWithNeighboringFiles.Files.First().Path);
        }
        else if (activatedEventArgs is IFileActivatedEventArgs fileActivatedEventArgs)
        {
            mediaItems = ConvertFilesToMediaItems(fileActivatedEventArgs.Files.Cast<IStorageFile>().ToList());
            startItem = mediaItems.First();
        }
        else
        {
#if DEBUG
            var files = await KnownFolders.PicturesLibrary.GetFilesAsync();
            mediaItems = ConvertFilesToMediaItems(files);
            startItem = mediaItems.FirstOrDefault();
#else
        mediaItems =  new List<IMediaItem>();
        startItem = null;
#endif
        }
        return new LoadMediaItemsResult(mediaItems, startItem);
    }

    public async Task<LoadMediaItemsResult> LoadMediaItems(StorageFolder storageFolder)
    {
        var files = await storageFolder.GetFilesAsync();
        var mediaItems = ConvertFilesToMediaItems(files);
        var startItem = mediaItems.FirstOrDefault();
        return new LoadMediaItemsResult(mediaItems, startItem);
    }

    private List<IMediaItem> ConvertFilesToMediaItems(IReadOnlyList<IStorageFile> files) 
    {
        return new List<IMediaItem>(files
            .Where(file => BitmapGraphicItem.SupportedFileExtensions.Contains(file.FileType.ToLower()))
            .Select(file => new BitmapGraphicItem(file)));
    }

}
