using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Messages;
using PhotoViewerCore.ViewModels;
using PhotoViewerCore.Utils;

namespace PhotoViewerApp.ViewModels;

public partial class BitmapFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsActive { get; set; } = false;

    public IBitmapImage? BitmapImage { get; private set; }

    public bool IsLoading { get; private set; } = false;

    public bool IsLoadingImageFailed { get; private set; } = false;

    public string ErrorMessage { get; private set; } = string.Empty;

    public MediaFileContextMenuModel ContextMenuModel { get; }

    private readonly IImageLoaderService imageLoadService;

    private CancellationTokenSource? cancellationTokenSource;

    public BitmapFlipViewItemModel(
        IMediaFileInfo mediaItem,
        MediaFileContextMenuModel mediaFileContextFlyoutModel,
        IMessenger messenger,
        IImageLoaderService imageLoadService) : base(messenger)
    {
        MediaItem = mediaItem;
        ContextMenuModel = mediaFileContextFlyoutModel;
        ContextMenuModel.Files = new[] { mediaItem };
        this.imageLoadService = imageLoadService;       
    }

    public async Task InitializeAsync()
    {
        Messenger.Register<BitmapRotatedMesssage>(this, OnBitmapRotatedMesssageReceived);
        await LoadImageAsync();
    }

    public void Cleanup()
    {
        Messenger.UnregisterAll(this);
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

            IsLoading = true;
            IsLoadingImageFailed = false;
            ErrorMessage = string.Empty;

            var bitmapImage = await imageLoadService.LoadFromFileAsync((IBitmapFileInfo)MediaItem, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            BitmapImage = bitmapImage;
            IsLoading = false;

            Messenger.Send(new BitmapImageLoadedMessage((IBitmapFileInfo)MediaItem, bitmapImage));

            Log.Info($"Loaded {MediaItem.Name} sucessfully");
        }
        catch (OperationCanceledException)
        {
            Log.Debug($"Canceled loading {MediaItem.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load {MediaItem.Name}", ex);
            IsLoading = false;
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
