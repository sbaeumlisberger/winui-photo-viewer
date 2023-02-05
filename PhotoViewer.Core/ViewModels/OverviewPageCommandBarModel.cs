using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;
using System.Windows.Input;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public interface IOverviewPageCommandBarModel : INotifyPropertyChanged
{
    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; }
}

public partial class OverviewPageCommandBarModel : ViewModelBase, IOverviewPageCommandBarModel
{
    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IAcceleratedCommand DeleteCommand { get; }

    public bool CanRotate => SelectedItems.Any() && SelectedItems.All(file => file is IBitmapFileInfo);

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly ApplicationSettings settings;

    public OverviewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        IDeleteFilesCommand deleteFilesCommand,
        IRotateBitmapService rotateBitmapService,
        ApplicationSettings settings) : base(messenger)
    {
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.rotateBitmapService = rotateBitmapService;
        this.settings = settings;
        DeleteCommand = deleteFilesCommand;
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
        var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async (bitmapFile) =>
        {
            await rotateBitmapService.RotateClockwise90DegreesAsync(bitmapFile).ConfigureAwait(false);
        });
        result.ProcessedElements.ForEach(bitmapFile => Messenger.Send(new BitmapRotatedMesssage(bitmapFile)));
    }

    [RelayCommand]
    private void ChangeThumbnailSize(double newThumbnailSize) 
    {
        Messenger.Send(new ChangeThumbnailSizeMessage(newThumbnailSize));
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
