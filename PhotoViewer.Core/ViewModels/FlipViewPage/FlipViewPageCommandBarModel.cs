using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.ViewModels.Shared;
using System.Windows.Input;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public interface IFlipViewPageCommandBarModel : IViewModel
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
    public IAcceleratedCommand PrefixFilesByDateCommand { get; }

    public bool CanStartDiashow => SelectedItemModel != null;

    public bool CanCropImage => SelectedItemModel?.MediaItem is IBitmapFileInfo;

    public bool CanRotate => SelectedItemModel?.MediaItem is IBitmapFileInfo bitmap && rotateBitmapService.CanRotate(bitmap);

    private bool CanNavigateToComparePage => SelectedItemModel?.MediaItem is IBitmapFileInfo;

    public BackgroundTasksViewModel BackgroundTasks { get; }

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly ApplicationSettings settings;

    internal FlipViewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        IRotateBitmapService rotatePhotoService,
        IViewModelFactory viewModelFactory,
        ICommand selectPreviousCommand,
        ICommand selectNextCommand,
        ApplicationSettings settings,
        IDeleteFilesCommand deleteFilesCommand,
        IMoveRawFilesToSubfolderCommand moveRawFilesToSubfolderCommand,
        IDeleteSingleRawFilesCommand deleteSingleRawFilesCommand,
        IShiftDatenTakenCommand shiftDatenTakenCommand,
        IImportGpxTrackCommand importGpxTrackCommand,
        IPrefixFilesByDateCommand prefixFilesByDateCommand) : base(messenger)
    {
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.rotateBitmapService = rotatePhotoService;
        this.settings = settings;

        DeleteCommand = deleteFilesCommand;
        SelectPreviousCommand = selectPreviousCommand;
        SelectNextCommand = selectNextCommand;
        MoveRawFilesToSubfolderCommand = moveRawFilesToSubfolderCommand;
        DeleteSingleRawFilesCommand = deleteSingleRawFilesCommand;
        ShiftDatenTakenCommand = shiftDatenTakenCommand;
        ImportGpxTrackCommand = importGpxTrackCommand;
        PrefixFilesByDateCommand = prefixFilesByDateCommand;

        BackgroundTasks = viewModelFactory.CreateBackgroundTasksViewModel();
    }

    [RelayCommand]
    private void NavigateToOverviewPage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(OverviewPageModel)));
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToComparePage))]
    private void NavigateToComparePage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(ComparePageModel), SelectedItemModel!.MediaItem));
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

    [RelayCommand(CanExecute = nameof(CanCropImage))]
    private void CropImage()
    {
        Messenger.Send(new ToggleCropImageToolMessage());
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        var bitmap = (IBitmapFileInfo)SelectedItemModel!.MediaItem;
        Log.Debug($"Execute rotate command for: {bitmap.DisplayName}");
        try
        {
            await rotateBitmapService.RotateClockwise90DegreesAsync(bitmap);
            Messenger.Send(new BitmapModifiedMesssage(bitmap));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to rotate {bitmap.FilePath}", ex);
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
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
