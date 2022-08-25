using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCoreModule.Model;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewerApp.ViewModels;

public interface IMediaFlipViewItemModel : INotifyPropertyChanged
{
    IMediaItem MediaItem { get; }

    IBitmapImage? BitmapImage { get; }

    Task<IBitmapImage?> WaitUntilImageLoaded();
}

public partial class MediaFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public event EventHandler<EventArgs>? Cleanup; // TODO

    public IMediaItem MediaItem { get; }

    [ObservableProperty]
    private IBitmapImage? bitmapImage;

    private readonly IImageLoadService imageLoadService;

    private readonly TaskCompletionSource<bool> loadImageTaskCompletionSource = new TaskCompletionSource<bool>();

    public MediaFlipViewItemModel(IMediaItem mediaItem, IImageLoadService imageLoadService)
    {    
        MediaItem = mediaItem;
        this.imageLoadService = imageLoadService;
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            BitmapImage = await imageLoadService.LoadFromFileAsync(MediaItem.File, CancellationToken.None);
            loadImageTaskCompletionSource.SetResult(true);
            Log.Info($"Loaded {MediaItem.Name} sucessfully");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load {MediaItem.Name}", ex);
            loadImageTaskCompletionSource.SetResult(false);       
        }
    }

    public async Task<IBitmapImage?> WaitUntilImageLoaded()
    {
        await loadImageTaskCompletionSource.Task;
        return BitmapImage;
    }
}
