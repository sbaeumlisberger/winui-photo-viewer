using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.Utils;
using Tocronx.SimpleAsync;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core;

namespace PhotoViewer.App.ViewModels;

public interface IBitmapFlipViewItemModel : IMediaFlipViewItemModel
{
    IBitmapImageModel? BitmapImage { get; }
}

public partial class BitmapFlipViewItemModel : ViewModelBase, IBitmapFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsSelected { get; set; } = false;

    public bool IsDiashowActive { get; set; }

    public IBitmapImageModel? BitmapImage { get; private set; }

    public bool IsLoading { get; private set; } = false;

    public bool IsLoadingImageFailed { get; private set; } = false;

    public string ErrorMessage { get; private set; } = string.Empty;

    public bool IsContextMenuEnabeld => !IsDiashowActive;

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public ITagPeopleToolModel? PeopleTagToolModel { get; }

    public bool CanTagPeople => PeopleTagToolModel != null;

    private readonly IImageLoaderService imageLoadService;

    private readonly CancelableTaskRunner loadImageRunner = new CancelableTaskRunner();

    public BitmapFlipViewItemModel(
        IBitmapFileInfo bitmapFile,
        IMediaFileContextMenuModel mediaFileContextFlyoutModel,
        IViewModelFactory viewModelFactory,
        IMessenger messenger,
        IImageLoaderService imageLoadService) : base(messenger, false)
    {
        this.imageLoadService = imageLoadService;

        MediaItem = bitmapFile;
        ContextMenuModel = mediaFileContextFlyoutModel;
        ContextMenuModel.Files = new[] { bitmapFile };

        if (bitmapFile.IsMetadataSupported)
        {
            PeopleTagToolModel = viewModelFactory.CreateTagPeopleToolModel(bitmapFile);
        }
    }

    partial void OnIsSelectedChanged()
    {
        if (PeopleTagToolModel != null)
        {
            PeopleTagToolModel.IsEnabled = IsSelected;
        }
    }

    public async Task InitializeAsync()
    {
        Messenger.Register<BitmapRotatedMesssage>(this, OnBitmapRotatedMesssageReceived);

        await LoadImageAsync();

        if (PeopleTagToolModel != null)
        {
            await PeopleTagToolModel.InitializeAsync();
        }
    }

    protected override void OnCleanup()
    {
        loadImageRunner.Cancel();
        var bitmapImage = BitmapImage;
        BitmapImage = null;
        bitmapImage?.Dispose();
        PeopleTagToolModel?.Cleanup();
    }

    private async void OnBitmapRotatedMesssageReceived(BitmapRotatedMesssage msg)
    {
        if (msg.Bitmap == MediaItem)
        {
            await LoadImageAsync();
        }
    }

    partial void OnBitmapImageChanged()
    {
        if (PeopleTagToolModel != null)
        {
            PeopleTagToolModel.BitmapImage = BitmapImage;
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

                var bitmapFile = (IBitmapFileInfo)MediaItem;

                var bitmapImage = await ImagePreloadService.Instance.GetPreloadedImageAsync(bitmapFile)
                                  ?? await imageLoadService.LoadFromFileAsync(bitmapFile, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                BitmapImage = bitmapImage;
                IsLoading = false;

                Messenger.Send(new BitmapImageLoadedMessage(bitmapFile, bitmapImage));
                Log.Info($"Loaded {MediaItem.DisplayName} sucessfully");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Failed to load {MediaItem.DisplayName}", ex);
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
