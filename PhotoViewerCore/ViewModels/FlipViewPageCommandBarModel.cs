using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Commands;
using PhotoViewerCore.Models;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
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

    public IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    public IAcceleratedCommand DeleteCommand { get; }

    public ICommand SelectPreviousCommand { get; }

    public ICommand SelectNextCommand { get; }

    public bool CanStartDiashow { get; private set; } = false;

    public bool CanRotate { get; private set; } = false;

    private readonly Session session;

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
        ApplicationSettings settings,
        IDeleteFilesCommand deleteFilesCommand) : base(messenger)
    {
        this.session = session;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.rotatePhotoService = rotatePhotoService;
        this.deleteMediaService = deleteMediaService;
        this.settings = settings;

        DeleteCommand = deleteFilesCommand;
        SelectPreviousCommand = flipViewModel.SelectPreviousCommand;
        SelectNextCommand = flipViewModel.SelectNextCommand;
    }

    partial void OnSelectedItemModelChanged()
    {
        CanStartDiashow = SelectedItemModel != null;
        CanRotate = SelectedItemModel?.MediaItem is IBitmapFileInfo bitmap && rotatePhotoService.CanRotate(bitmap);
    }

    [RelayCommand]
    private void NavigateToOverviewPage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(OverviewPageModel)));
    }

    [RelayCommand(CanExecute = nameof(CanStartDiashow))]
    private void StartDiashow()
    {
        Messenger.Send(new StartDiashowMessage());
    }

    [RelayCommand]
    private void ToggleMetadataPanel() 
    {
        Messenger.Send(new ToggleMetataPanelMessage());
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        var bitmap = (IBitmapFileInfo)SelectedItemModel!.MediaItem;
        await rotatePhotoService.RotateClockwise90DegreesAsync(bitmap);
        Messenger.Send(new BitmapRotatedMesssage(bitmap));
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
            Messenger.Send(new MediaFilesLoadedMessage(result.MediaItems, result.StartItem));
        }
    }

    [RelayCommand]
    private void NavigateToSettingsPage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(SettingsPageModel)));
    }
}
