using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.ViewModels;

public partial class BitmapFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private IBitmapImage? bitmapImage;

    private readonly IImageLoaderService imageLoadService;

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    private TaskCompletionSource<bool> loadImageTaskCompletionSource = new TaskCompletionSource<bool>();

    public BitmapFlipViewItemModel(IMediaFileInfo mediaItem, IMessenger messenger, IImageLoaderService imageLoadService)
    {
        MediaItem = mediaItem;
        this.imageLoadService = imageLoadService;

        messenger.Subscribe<BitmapRotatedMesssage>(msg =>
        {
            if (msg.Bitmap == MediaItem && BitmapImage != null)
            {
                StartLoading();
            }
        });
    }

    public async void StartLoading()
    {
        var cancellationToken = cancellationTokenSource.Token;
        try
        {
            loadImageTaskCompletionSource = new TaskCompletionSource<bool>();
            BitmapImage = await imageLoadService.LoadFromFileAsync(MediaItem.File, cancellationToken);
            loadImageTaskCompletionSource.SetResult(true);
            Log.Info($"Loaded {MediaItem.Name} sucessfully");
        }
        catch (OperationCanceledException)
        {
            Log.Debug($"Canceled loading {MediaItem.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load {MediaItem.Name}", ex);
            loadImageTaskCompletionSource.SetResult(false);
        }
    }

    public void Cleanup()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource = new CancellationTokenSource();
        BitmapImage?.Dispose();
        BitmapImage = null;
    }

    public async Task<IBitmapImage?> WaitUntilImageLoaded()
    {
        await loadImageTaskCompletionSource.Task;
        return BitmapImage;
    }

}
