using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using System.Collections.Generic;
using System.ComponentModel;
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

    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [ObservableProperty]
    private bool canDelete = false;


    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly ILoadMediaItemsService loadMediaItemsService;

    public FlipViewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        ILoadMediaItemsService loadMediaItemsService,
        ICommand selectPreviousCommand,
        ICommand selectNextCommand)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        SelectPreviousCommand = selectPreviousCommand;
        SelectNextCommand = selectNextCommand;
    }

    partial void OnSelectedItemModelChanged(IMediaFlipViewItemModel? value)
    {
        CanDelete = SelectedItemModel != null;
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        SelectedItemModel!.MediaItem.DeleteAsync();
        messenger.Publish(new MediaItemsDeletedMessage(new List<IMediaItem>() { SelectedItemModel!.MediaItem }));
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
