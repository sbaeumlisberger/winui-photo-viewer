using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Messages;
using PhotoViewerCore.ViewModels;
using System.Threading;
using Windows.Foundation;

namespace PhotoViewerApp.ViewModels;

public partial class BitmapFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsActive { get; set; } = false;

    public IBitmapImage? BitmapImage { get; private set; }

    public bool IsLoadingImageFailed { get; private set; } = false;

    public string ErrorMessage { get; private set; } = string.Empty;

    private readonly IMessenger messenger;

    private readonly IImageLoaderService imageLoadService;

    private CancellationTokenSource? cancellationTokenSource;

    public BitmapFlipViewItemModel(IMediaFileInfo mediaItem, IMessenger messenger, IImageLoaderService imageLoadService)
    {
        MediaItem = mediaItem;
        this.messenger = messenger;
        this.imageLoadService = imageLoadService;
        messenger.Subscribe<BitmapRotatedMesssage>(OnBitmapRotatedMesssageReceived);
    }

    public async Task InitializeAsync()
    {
        await LoadImageAsync();
    }

    public void Cleanup()
    {
        messenger.Unsubscribe<BitmapRotatedMesssage>(OnBitmapRotatedMesssageReceived);
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        var bitmapImage = BitmapImage;
        BitmapImage = null;
        bitmapImage?.Dispose();
    }

    private async void OnBitmapRotatedMesssageReceived(BitmapRotatedMesssage msg) 
    {
        if (msg.Bitmap == MediaItem)
        {
            await LoadImageAsync();
        }
    }
 
    private async Task LoadImageAsync() 
    {
        try
        {
            cancellationTokenSource?.Cancel();

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            IsLoadingImageFailed = false;
            ErrorMessage = string.Empty;

            var bitmapImage = await imageLoadService.LoadFromFileAsync((IBitmapFileInfo)MediaItem, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            BitmapImage = bitmapImage;

            messenger.Publish(new BitmapImageLoadedMessage((IBitmapFileInfo)MediaItem, bitmapImage));

            Log.Info($"Loaded {MediaItem.Name} sucessfully");
        }
        catch (OperationCanceledException)
        {
            Log.Debug($"Canceled loading {MediaItem.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load {MediaItem.Name}", ex);
            IsLoadingImageFailed = true;
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(IsLoadingImageFailed))]
    private async Task ReloadAsync()
    {
        await LoadImageAsync();
    }

}
