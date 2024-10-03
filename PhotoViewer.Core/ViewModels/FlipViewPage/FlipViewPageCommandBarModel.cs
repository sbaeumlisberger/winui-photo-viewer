using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.ViewModels.Shared;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public interface IFlipViewPageCommandBarModel : IViewModel
{
    IMediaFlipViewItemModel? SelectedItemModel { get; set; }
}

public partial class FlipViewPageCommandBarModel : ViewModelBase, IFlipViewPageCommandBarModel
{
    public IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    public IDeleteFilesCommand DeleteCommand { get; }

    public ICommand SelectPreviousCommand { get; }

    public ICommand SelectNextCommand { get; }

    public bool CanStartDiashow => SelectedItemModel != null;

    public bool CanCropImage => SelectedItemModel?.MediaFile is IBitmapFileInfo bitmapFileInfo && IsSupportedByBitmapEncoder(bitmapFileInfo);

    public bool CanEditImage => SelectedItemModel?.MediaFile is IBitmapFileInfo bitmapFileInfo && IsSupportedByBitmapEncoder(bitmapFileInfo);

    public bool CanRotate => SelectedItemModel?.MediaFile is IBitmapFileInfo bitmap && rotateBitmapService.CanRotate(bitmap);

    private bool CanNavigateToComparePage => SelectedItemModel?.MediaFile is IBitmapFileInfo;

    public BackgroundTasksViewModel BackgroundTasks { get; }

    public SortMenuModel SortMenuModel { get; }

    public ToolsMenuModel ToolsMenuModel { get; }

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly ApplicationSettings settings;

    private HashSet<string>? contentTypesSupportedByBitmapEncoder;

    internal FlipViewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService mediaFilesLoaderService,
        IRotateBitmapService rotatePhotoService,
        IViewModelFactory viewModelFactory,
        ICommand selectPreviousCommand,
        ICommand selectNextCommand,
        ApplicationSettings settings,
        IDeleteFilesCommand deleteFilesCommand)
     : base(messenger)
    {
        this.dialogService = dialogService;
        this.mediaFilesLoaderService = mediaFilesLoaderService;
        this.rotateBitmapService = rotatePhotoService;
        this.settings = settings;

        DeleteCommand = deleteFilesCommand;
        SelectPreviousCommand = selectPreviousCommand;
        SelectNextCommand = selectNextCommand;

        BackgroundTasks = viewModelFactory.CreateBackgroundTasksViewModel();

        SortMenuModel = viewModelFactory.CreateSortMenuModel();
        ToolsMenuModel = viewModelFactory.CreateToolsMenuModel();
    }

    [RelayCommand]
    private void NavigateToOverviewPage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(OverviewPageModel)));
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToComparePage))]
    private void NavigateToComparePage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(ComparePageModel), SelectedItemModel!.MediaFile));
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

    [RelayCommand(CanExecute = nameof(CanEditImage))]
    private void EditImage()
    {
        Messenger.Send(new ToggleEditImageOverlayMessage());
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        var bitmap = (IBitmapFileInfo)SelectedItemModel!.MediaFile;
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
            var loadMediaFilesTask = mediaFilesLoaderService.LoadFolder(folder, config);
            Messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
        }
    }

    [RelayCommand]
    private void NavigateToSettingsPage()
    {
        Messenger.Send(new NavigateToPageMessage(typeof(SettingsPageModel)));
    }

    private bool IsSupportedByBitmapEncoder(IBitmapFileInfo bitmapFileInfo)
    {
        contentTypesSupportedByBitmapEncoder ??= BitmapEncoder.GetEncoderInformationEnumerator().SelectMany(info => info.MimeTypes).ToHashSet();
        return contentTypesSupportedByBitmapEncoder.Contains(bitmapFileInfo.ContentType);
    }

}
