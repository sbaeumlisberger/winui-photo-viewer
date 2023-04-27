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

    public ICropImageToolModel CropImageToolModel { get; }

    public bool CanTagPeople => PeopleTagToolModel != null;

    private readonly IImageLoaderService imageLoadService;

    private readonly CancelableTaskRunner loadImageRunner = new CancelableTaskRunner();

    public BitmapFlipViewItemModel(
        IBitmapFileInfo bitmapFile,
        IMediaFileContextMenuModel mediaFileContextFlyoutModel,
        IViewModelFactory viewModelFactory,
        IMessenger messenger,
        IImageLoaderService imageLoadService) : base(messenger)
    {
        this.imageLoadService = imageLoadService;

        MediaItem = bitmapFile;
        ContextMenuModel = mediaFileContextFlyoutModel;
        ContextMenuModel.Files = new[] { bitmapFile };

        if (bitmapFile.IsMetadataSupported)
        {
            PeopleTagToolModel = viewModelFactory.CreateTagPeopleToolModel(bitmapFile);
        }

        CropImageToolModel = viewModelFactory.CreateCropImageToolModel(bitmapFile);
    }

    partial void OnIsSelectedChanged()
    {
        if (PeopleTagToolModel != null)
        {
            PeopleTagToolModel.IsEnabled = IsSelected;
        }
        CropImageToolModel.IsEnabled = IsSelected;        
    }

    public async Task InitializeAsync()
    {
        Register<BitmapModifiedMesssage>(OnReceive);

        var loadImageTask = LoadImageAsync();

        if (PeopleTagToolModel != null)
        {
            await PeopleTagToolModel.InitializeAsync();
        }

        await loadImageTask;
    }

    protected override void OnCleanup()
    {
        loadImageRunner.Cancel();
        var bitmapImage = BitmapImage;
        BitmapImage = null;
        bitmapImage?.Dispose();
        PeopleTagToolModel?.Cleanup();
        CropImageToolModel.Cleanup();
        ContextMenuModel.Cleanup();
    }

    private async void OnReceive(BitmapModifiedMesssage msg)
    {
        if (msg.BitmapFile.Equals(MediaItem))
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

                if (cancellationToken.IsCancellationRequested)
                {
                    bitmapImage.Dispose();
                }
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
