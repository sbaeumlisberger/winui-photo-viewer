using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Commands;
using PhotoViewerCore.Models;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using System.ComponentModel;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public interface IOverviewPageCommandBarModel : INotifyPropertyChanged
{
    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; }
}

public partial class OverviewPageCommandBarModel : ViewModelBase, IOverviewPageCommandBarModel
{
    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public bool CanDelete { get; private set; } = false;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly IDeleteFilesCommand deleteFilesCommand;

    private readonly ApplicationSettings settings;

    public OverviewPageCommandBarModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        IDeleteFilesCommand deleteFilesCommand,
        ApplicationSettings settings)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.deleteFilesCommand = deleteFilesCommand;
        this.settings = settings;
    }

    partial void OnSelectedItemsChanged()
    {
        CanDelete = SelectedItems.Any();
    }

    [RelayCommand]
    private void ToggleMetadataPanel() 
    {
        messenger.Publish(new ToggleMetataPanelMessage());
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        await deleteFilesCommand.ExecuteAsync(SelectedItems.ToList());
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
            messenger.Publish(new MediaItemsLoadedMessage(result.MediaItems, result.StartItem));
        }
    }

    [RelayCommand]
    private void NavigateToSettingsPage()
    {
        messenger.Publish(new NavigateToPageMessage(typeof(SettingsPageModel)));
    }
}
