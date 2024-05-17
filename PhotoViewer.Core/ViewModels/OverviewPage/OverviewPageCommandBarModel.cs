using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.ViewModels.Shared;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public interface IOverviewPageCommandBarModel : IViewModel
{
    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; }
}

public partial class OverviewPageCommandBarModel : ViewModelBase, IOverviewPageCommandBarModel
{
    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IAcceleratedCommand DeleteCommand { get; }

    public bool CanRotate => SelectedItems.Any() && SelectedItems.All(file => file is IBitmapFileInfo);

    public BackgroundTasksViewModel BackgroundTasks { get; }

    public SortMenuModel SortMenuModel { get; }

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly ApplicationSettings settings;

    internal OverviewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService mediaFilesLoaderService,
        IDeleteFilesCommand deleteFilesCommand,
        IRotateBitmapService rotateBitmapService,
        IViewModelFactory viewModelFactory,
        ApplicationSettings settings) : base(messenger)
    {
        this.dialogService = dialogService;
        this.mediaFilesLoaderService = mediaFilesLoaderService;
        this.rotateBitmapService = rotateBitmapService;
        this.settings = settings;
        DeleteCommand = deleteFilesCommand;

        BackgroundTasks = viewModelFactory.CreateBackgroundTasksViewModel();

        SortMenuModel = viewModelFactory.CreateSortMenuModel();
    }

    [RelayCommand]
    private void ToggleMetadataPanel()
    {
        Messenger.Send(new ToggleMetataPanelMessage());
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        var files = SelectedItems.OfType<IBitmapFileInfo>().ToList();
        var result = await files.Parallel().TryProcessAsync(async (bitmapFile) =>
        {
            await rotateBitmapService.RotateClockwise90DegreesAsync(bitmapFile).ConfigureAwait(false);
        });
        result.ProcessedElements.ForEach(bitmapFile => Messenger.Send(new BitmapModifiedMesssage(bitmapFile)));
    }

    [RelayCommand]
    private void ChangeThumbnailSize(double newThumbnailSize)
    {
        Messenger.Send(new ChangeThumbnailSizeMessage(newThumbnailSize));
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
}
