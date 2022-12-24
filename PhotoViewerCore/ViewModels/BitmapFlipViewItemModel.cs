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
using Tocronx.SimpleAsync;

namespace PhotoViewerApp.ViewModels;

public partial class BitmapFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsActive { get; set; } = false;

    public bool IsDiashowActive { get; set; }

    public IBitmapImage? BitmapImage { get; private set; }

    public bool IsLoading { get; private set; } = false;

    public bool IsLoadingImageFailed { get; private set; } = false;

    public string ErrorMessage { get; private set; } = string.Empty;

    [DependsOn(nameof(IsDiashowActive))]
    public bool IsContextMenuEnabeld => !IsDiashowActive;

    public MediaFileContextMenuModel ContextMenuModel { get; }

    private readonly IImageLoaderService imageLoadService;

    private readonly CancelableTaskRunner loadImageRunner = new CancelableTaskRunner();

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

    public async Task PrepareAsync()
    {
        Messenger.Register<BitmapRotatedMesssage>(this, OnBitmapRotatedMesssageReceived);
        await LoadImageAsync();
    }

    public void Cleanup()
    {
        Messenger.UnregisterAll(this);
        loadImageRunner.Cancel();
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
        await loadImageRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            try
            {
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Failed to load {MediaItem.Name}", ex);
                IsLoading = false;
                IsLoadingImageFailed = true;
                ErrorMessage = ex.Message;
            }
        });
    }

    [RelayCommand(CanExecute = nameof(IsLoadingImageFailed))]
    private async Task ReloadAsync()
    {
        await LoadImageAsync();
    }

}
