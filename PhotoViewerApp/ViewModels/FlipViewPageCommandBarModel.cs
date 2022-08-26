using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
}

public partial class FlipViewPageCommandBarModel : ViewModelBase, IFlipViewPageCommandBarModel
{

    [ObservableProperty]
    private IMediaFlipViewItemModel? selectedItemModel;

    public ICommand SelectPreviousCommand { get; }

    public ICommand SelectNextCommand { get; }

    public ICommand StartDiashowCommand { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RotateCommand))]
    private bool canRotate = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool canDelete = false;

    private readonly Session session;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly ILoadMediaItemsService loadMediaItemsService;

    public FlipViewPageCommandBarModel(
        Session session,
        IMessenger messenger,
        IDialogService dialogService,
        ILoadMediaItemsService loadMediaItemsService,
        IMediaFlipViewModel flipViewModel)
    {
        this.session = session;
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
     
        SelectPreviousCommand = flipViewModel.SelectPreviousCommand;
        SelectNextCommand = flipViewModel.SelectNextCommand;
        StartDiashowCommand = null!; // TODO
    }

    partial void OnSelectedItemModelChanged(IMediaFlipViewItemModel? value)
    {
        CanDelete = SelectedItemModel != null;
        CanRotate = false;
    }

    [RelayCommand]
    private void NavigateToOverviewPage()
    {
       messenger.Publish(new NavigateToPageMessage(typeof(OverviewPageModel)));
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        // TODO
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        await SelectedItemModel!.MediaItem.DeleteAsync();
        session.MediaItems.Remove(SelectedItemModel.MediaItem);
        messenger.Publish(new MediaItemsDeletedMessage(new List<IMediaItem>() { SelectedItemModel.MediaItem }));
    }

    [RelayCommand]
    private async void OpenFolder()
    {
        var folderPickerModel = new FolderPickerModel();
        await dialogService.ShowDialogAsync(folderPickerModel);
        if (folderPickerModel.Folder is StorageFolder folder)
        {
            var result = await loadMediaItemsService.LoadMediaItems(folder);
            messenger.Publish(new MediaItemsLoadedMessage(result.MediaItems, result.StartItem));
        }
    }
}
