using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewerApp.ViewModels;

public interface IMediaFlipViewItemModel : INotifyPropertyChanged
{
    IMediaItem MediaItem { get; }

    IBitmapImage? BitmapImage { get; }

    void StartLoading();

    void Cleanup();

    Task<IBitmapImage?> WaitUntilImageLoaded();
}

public partial class MediaFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public event EventHandler<EventArgs>? Cleanuped; // TODO

    public IMediaItem MediaItem { get; }

    [ObservableProperty]
    private IBitmapImage? bitmapImage;

    private readonly IImageLoadService imageLoadService;

    private TaskCompletionSource<bool> loadImageTaskCompletionSource = new TaskCompletionSource<bool>();

    public MediaFlipViewItemModel(IMediaItem mediaItem, IImageLoadService imageLoadService)
    {
        MediaItem = mediaItem;
        this.imageLoadService = imageLoadService;
    }

    public async void StartLoading()
    {
        // TODO wait for cleanup

        loadImageTaskCompletionSource = new TaskCompletionSource<bool>();
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

    public async void Cleanup()
    {
        await WaitUntilImageLoaded();
        BitmapImage?.Dispose();
        BitmapImage = null;
    }

    public async Task<IBitmapImage?> WaitUntilImageLoaded()
    {
        await loadImageTaskCompletionSource.Task;
        return BitmapImage;
    }

}
