using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVieweApp.Services;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
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

    [ObservableProperty]
    private IMediaFlipViewItemModel? selectedItemModel;

    [ObservableProperty]
    private bool isVisible = true;

    public ICommand SelectPreviousCommand { get; }

    public ICommand SelectNextCommand { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartDiashowCommand))]
    private bool canStartDiashow = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RotateCommand))]
    private bool canRotate = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool canDelete = false;

    private readonly Session session;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly IRotateBitmapService rotatePhotoService;

    private readonly IDeleteMediaService deleteMediaService;

    public FlipViewPageCommandBarModel(
        Session session,
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        IRotateBitmapService rotatePhotoService,
        IMediaFlipViewModel flipViewModel,
        IDeleteMediaService deleteMediaService)
    {
        this.session = session;
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.rotatePhotoService = rotatePhotoService;
        this.deleteMediaService = deleteMediaService;

        SelectPreviousCommand = flipViewModel.SelectPreviousCommand;
        SelectNextCommand = flipViewModel.SelectNextCommand;
        this.deleteMediaService = deleteMediaService;
    }

    partial void OnSelectedItemModelChanged(IMediaFlipViewItemModel? value)
    {
        CanStartDiashow = SelectedItemModel != null;
        CanDelete = SelectedItemModel != null;
        CanRotate = SelectedItemModel?.MediaItem is BitmapFileInfo bitmap && rotatePhotoService.CanRotate(bitmap);
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
        var bitmap = (BitmapFileInfo)SelectedItemModel!.MediaItem;
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
            var result = await loadMediaItemsService.LoadMediaFilesAsync(folder, /*TODO*/new LoadMediaConfig(true, "RAWs"));
            messenger.Publish(new MediaItemsLoadedMessage(result.MediaItems, result.StartItem));
        }
    }
}
