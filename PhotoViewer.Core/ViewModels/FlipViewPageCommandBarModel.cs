using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

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

    public IAcceleratedCommand MoveRawFilesToSubfolderCommand { get; }
    public IAcceleratedCommand DeleteSingleRawFilesCommand { get; }
    public IAcceleratedCommand ShiftDatenTakenCommand { get; }
    public IAcceleratedCommand ImportGpxTrackCommand { get; }

    public bool CanStartDiashow { get; private set; } = false;

    public bool CanRotate { get; private set; } = false;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly IRotateBitmapService rotatePhotoService;

    private readonly ApplicationSettings settings;

    internal FlipViewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        IRotateBitmapService rotatePhotoService,
        ICommand selectPreviousCommand,
        ICommand selectNextCommand,
        ApplicationSettings settings,
        IDeleteFilesCommand deleteFilesCommand,
        IMoveRawFilesToSubfolderCommand moveRawFilesToSubfolderCommand,
        IDeleteSingleRawFilesCommand deleteSingleRawFilesCommand,
        IShiftDatenTakenCommand shiftDatenTakenCommand,
        IImportGpxTrackCommand importGpxTrackCommand) : base(messenger)
    {
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.rotatePhotoService = rotatePhotoService;
        this.settings = settings;

        DeleteCommand = deleteFilesCommand;
        SelectPreviousCommand = selectPreviousCommand;
        SelectNextCommand = selectNextCommand;
        MoveRawFilesToSubfolderCommand = moveRawFilesToSubfolderCommand;
        DeleteSingleRawFilesCommand = deleteSingleRawFilesCommand;
        ShiftDatenTakenCommand= shiftDatenTakenCommand;
        ImportGpxTrackCommand = importGpxTrackCommand;
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
        try
        {
            await rotatePhotoService.RotateClockwise90DegreesAsync(bitmap);
            Messenger.Send(new BitmapRotatedMesssage(bitmap));
        }
        catch (Exception ex) 
        {
            Log.Error($"Failed to rotate {bitmap.FilePath}", ex);
        }
    }

    [RelayCommand]
    private async void OpenFolder()
    {
        var folderPickerModel = new FolderPickerModel();
        await dialogService.ShowDialogAsync(folderPickerModel);
        if (folderPickerModel.Folder is StorageFolder folder)
        {
            var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);
            var loadMediaFilesTask = loadMediaItemsService.LoadFolder(folder, config);
            Messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
        }
    }

    [RelayCommand]
    private void NavigateToSettingsPage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(SettingsPageModel)));
    }
}
