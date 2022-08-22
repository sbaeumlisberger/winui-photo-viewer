using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Windows.Storage;

//namespace PhotoViewerApp
//{
//    public partial class Ioc

//    {
//        public virtual IFlipViewPageCommandBarModel CreateFlipViewPageCommandBarModel(ICommand selectPreviousCommand, ICommand selectNextCommand)
//        {
//            return new FlipViewPageCommandBarModel(CreateLoadMediaItemsService(), selectPreviousCommand, selectNextCommand);
//        }
//    }
//}

namespace PhotoViewerApp.ViewModels
{
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

        private readonly ILoadMediaItemsService loadMediaItemsService;

        public FlipViewPageCommandBarModel(ILoadMediaItemsService loadMediaItemsService, ICommand selectPreviousCommand, ICommand selectNextCommand)
        {
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
            Publish(new MediaItemsDeletedMessage(new List<IMediaItem>() { SelectedItemModel!.MediaItem }));
        }

        [RelayCommand]
        private async void OpenFolder()
        {
            var folderPickerModel = new FolderPickerModel();
            await ShowDialogAsync(folderPickerModel);
            if (folderPickerModel.Folder is StorageFolder folder)
            {
                await loadMediaItemsService.LoadMediaItems(folder);
            }
        }
    }
}
