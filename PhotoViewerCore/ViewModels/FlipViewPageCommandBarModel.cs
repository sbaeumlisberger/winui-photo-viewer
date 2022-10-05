using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Models;
using PhotoViewerCore.ViewModels;
using System.ComponentModel;
using System.Windows.Input;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public interface IFlipViewPageCommandBarModel : INotifyPropertyChanged
{
    IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    bool IsVisible { get; set; }
}

public partial class FlipViewPageCommandBarModel : ViewModelBase, IFlipViewPageCommandBarModel
{

    public IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    public bool IsVisible { get; set; } = true;

    public ICommand SelectPreviousCommand { get; }

    public ICommand SelectNextCommand { get; }

    public bool CanStartDiashow { get; private set; } = false;

    public bool CanRotate { get; private set; } = false;

    public bool CanDelete { get; private set; } = false;

    private readonly Session session;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly IRotateBitmapService rotatePhotoService;

    private readonly IDeleteMediaService deleteMediaService;

    private readonly ApplicationSettings settings;

    public FlipViewPageCommandBarModel(
        Session session,
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        IRotateBitmapService rotatePhotoService,
        IMediaFlipViewModel flipViewModel,
        IDeleteMediaService deleteMediaService,
        ApplicationSettings settings)
    {
        this.session = session;
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.rotatePhotoService = rotatePhotoService;
        this.deleteMediaService = deleteMediaService;
        this.settings = settings;

        SelectPreviousCommand = flipViewModel.SelectPreviousCommand;
        SelectNextCommand = flipViewModel.SelectNextCommand;
    }

    partial void OnSelectedItemModelChanged()
    {
        CanStartDiashow = SelectedItemModel != null;
        CanDelete = SelectedItemModel != null;
        CanRotate = SelectedItemModel?.MediaItem is IBitmapFileInfo bitmap && rotatePhotoService.CanRotate(bitmap);
    }

    [RelayCommand]
    private void NavigateToOverviewPage()
    {
        messenger.Publish(new NavigateToPageMessage(typeof(OverviewPageModel)));
    }

    [RelayCommand(CanExecute = nameof(CanStartDiashow))]
    private void StartDiashow()
    {
        messenger.Publish(new StartDiashowMessage());
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        var bitmap = (IBitmapFileInfo)SelectedItemModel!.MediaItem;
        await rotatePhotoService.RotateClockwise90DegreesAsync(bitmap);
        messenger.Publish(new BitmapRotatedMesssage(bitmap));
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        await deleteMediaService.DeleteMediaAsync(SelectedItemModel!.MediaItem, true/*TODO*/);
        session.MediaItems.Remove(SelectedItemModel.MediaItem);
        messenger.Publish(new MediaItemsDeletedMessage(new List<IMediaFileInfo>() { SelectedItemModel.MediaItem }));
    }

    [RelayCommand]
    private async void OpenFolder()
    {
        var folderPickerModel = new FolderPickerModel();
        await dialogService.ShowDialogAsync(folderPickerModel);
        if (folderPickerModel.Folder is StorageFolder folder)
        {
            var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName);
            var result = await loadMediaItemsService.LoadMediaFilesAsync(folder, config);
            messenger.Publish(new MediaItemsLoadedMessage(result.MediaItems, result.StartItem));
        }
    }

    [RelayCommand]
    private void NavigateToSettingsPage()
    {
        messenger.Publish(new NavigateToPageMessage(typeof(SettingsPageModel)));
    }
}
