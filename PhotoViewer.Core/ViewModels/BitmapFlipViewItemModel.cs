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

namespace PhotoViewer.App.ViewModels;

public partial class BitmapFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsSelected { get; set; } = false;

    public bool IsDiashowActive { get; set; }

    public IBitmapImage? BitmapImage { get; private set; }

    public bool IsLoading { get; private set; } = false;

    public bool IsLoadingImageFailed { get; private set; } = false;

    public string ErrorMessage { get; private set; } = string.Empty;

    public bool IsContextMenuEnabeld => !IsDiashowActive;

    public MediaFileContextMenuModel ContextMenuModel { get; }

    public TagPeopleToolModel? PeopleTagToolModel { get; }

    public bool CanTagPeople => PeopleTagToolModel != null;

    private readonly IImageLoaderService imageLoadService;

    private readonly CancelableTaskRunner loadImageRunner = new CancelableTaskRunner();

    public BitmapFlipViewItemModel(
        IBitmapFileInfo bitmapFile,
        MediaFileContextMenuModel mediaFileContextFlyoutModel,
        Func<IBitmapFileInfo, TagPeopleToolModel> peopleTagToolModelFactory,
        IMessenger messenger,
        IImageLoaderService imageLoadService) : base(messenger)
    {
        this.imageLoadService = imageLoadService;

        MediaItem = bitmapFile;
        ContextMenuModel = mediaFileContextFlyoutModel;
        ContextMenuModel.Files = new[] { bitmapFile };

        if (bitmapFile.IsMetadataSupported)
        {
            PeopleTagToolModel = peopleTagToolModelFactory.Invoke(bitmapFile);
        }
    }

    partial void OnIsSelectedChanged()
    {
        if (PeopleTagToolModel != null) 
        {
            PeopleTagToolModel.IsEnabeld = IsSelected;
        }
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
