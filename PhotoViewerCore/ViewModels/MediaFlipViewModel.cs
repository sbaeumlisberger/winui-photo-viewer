using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Models;
using PhotoViewerCore.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public interface IMediaFlipViewModel : INotifyPropertyChanged
{
    IMediaFlipViewItemModel? SelectedItemModel { get; }
    IRelayCommand SelectPreviousCommand { get; }
    IRelayCommand SelectNextCommand { get; }
    void SetItems(IEnumerable<IMediaFileInfo> mediaItems, IMediaFileInfo? startItem = null);
}

public partial class MediaFlipViewModel : ViewModelBase, IMediaFlipViewModel
{
    private static readonly int ImageCacheSize = 2;

    [ObservableProperty]
    private ObservableCollection<IMediaFlipViewItemModel> items = new ObservableCollection<IMediaFlipViewItemModel>();

    [ObservableProperty]
    private IMediaFlipViewItemModel? selectedItemModel; // restore on back nav

    [ObservableProperty]
    private bool showNoItemsUI = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectPreviousCommand))]
    private bool canSelectPrevious = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectNextCommand))]
    private bool canSelectNext = false;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly ApplicationSettings settings;

    private ICollection<IMediaFlipViewItemModel> loadedItemModels = Array.Empty<IMediaFlipViewItemModel>();

    private bool diashowActive = false;

    public MediaFlipViewModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory,
        ApplicationSettings settings)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.mediaFlipViewItemModelFactory = mediaFlipViewItemModelFactory;
        this.settings = settings;

        messenger.Subscribe<MediaItemsLoadedMessage>(msg =>
        {
            Log.Debug("MediaItemsLoadedMessage received");
            SetItems(msg.MediaItems, msg.StartItem);
        });

        messenger.Subscribe<MediaItemsDeletedMessage>(msg =>
        {
            Items.RemoveAll(itemModel => msg.MediaItems.Contains(itemModel.MediaItem));
        });

        messenger.Subscribe<StartDiashowMessage>(msg =>
        {
            diashowActive = true;
            DiashowLoop();
        });

        messenger.Subscribe<ExitDiashowMessage>(msg =>
        {
            diashowActive = false;
        });
    }

    public void SetItems(IEnumerable<IMediaFileInfo> mediaItems, IMediaFileInfo? startItem = null)
    {
        Log.Debug("SetItems called");
        Stopwatch sw = Stopwatch.StartNew();
        Items = new ObservableCollection<IMediaFlipViewItemModel>(mediaItems.Select(CreateFlipViewItemModel));
        sw.Stop();
        Log.Info($"Create {Items.Count} item models took {sw.ElapsedMilliseconds} ms");
        Log.Debug("Set SelectedItemModel");
        SelectedItemModel = startItem != null ? Items.FirstOrDefault(itemModel => itemModel.MediaItem == startItem) : Items.FirstOrDefault();
    }

    partial void OnItemsChanged(ObservableCollection<IMediaFlipViewItemModel> value)
    {
        ShowNoItemsUI = !Items.Any();
    }

    partial void OnSelectedItemModelChanged(IMediaFlipViewItemModel? value)
    {
        Log.Info($"Selection changed to {SelectedItemModel?.MediaItem.Name}");

        UpdateFlipViewItemModels();

        CanSelectPrevious = SelectedItemModel != null && Items.IndexOf(SelectedItemModel) > 0;
        CanSelectNext = SelectedItemModel != null && Items.IndexOf(SelectedItemModel) < Items.Count - 1;

        if (SelectedItemModel != null)
        {
            loadedItemModels.ForEach(x => x.IsActive = false);
            SelectedItemModel.IsActive = true;
        }
    }

    private async void DiashowLoop()
    {
        await Task.Delay(settings.DiashowTime);

        if (diashowActive)
        {
            if (CanSelectNext)
            {
                SelectNext();
            }
            else
            {
                SelectedItemModel = Items.First();
            }
            DiashowLoop();
        }
    }

    private void UpdateFlipViewItemModels()
    {
        ICollection<IMediaFlipViewItemModel> itemModelsToBeLoaded;

        if (SelectedItemModel is not null)
        {
            int selectedIndex = Items.IndexOf(SelectedItemModel);

            int startIndex = Math.Max(selectedIndex - ImageCacheSize, 0);
            int endIndex = Math.Min(selectedIndex + ImageCacheSize, Items.Count - 1);
            itemModelsToBeLoaded = Items.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        }
        else
        {
            itemModelsToBeLoaded = Array.Empty<IMediaFlipViewItemModel>();
        }

        foreach (var itemModel in loadedItemModels.Except(itemModelsToBeLoaded))
        {
            Log.Info($"Cleanup ViewModel for {itemModel.MediaItem.Name}");
            itemModel.Cleanup();
        }

        foreach (var itemModel in itemModelsToBeLoaded.Except(loadedItemModels))
        {
            Log.Info($"Load ViewModel for {itemModel.MediaItem.Name}");
            itemModel.StartLoading();
        }

        loadedItemModels = itemModelsToBeLoaded;
    }

    private IMediaFlipViewItemModel CreateFlipViewItemModel(IMediaFileInfo mediaFile)
    {
        return mediaFile switch
        {
            IBitmapFileInfo => mediaFlipViewItemModelFactory.Invoke(mediaFile),
            IVideoFileInfo => new VideoFlipViewItemModel(mediaFile), // TODO,
            IVectorGraphicFileInfo => new VectorGraphicFlipViewItemModel(mediaFile), // TODO
            _ => throw new Exception($"Unexcpected type of media file: {mediaFile.GetType()}")
        };
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevious))]
    private void SelectPrevious()
    {
        SelectedItemModel = Items[Items.IndexOf(SelectedItemModel!) - 1];
    }

    [RelayCommand(CanExecute = nameof(CanSelectNext))]
    private void SelectNext()
    {
        SelectedItemModel = Items[Items.IndexOf(SelectedItemModel!) + 1];
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
}
