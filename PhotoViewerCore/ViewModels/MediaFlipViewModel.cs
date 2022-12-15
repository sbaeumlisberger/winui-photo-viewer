using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
using PhotoViewerCore.Utils;
using Microsoft.Graphics.Canvas.Text;

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
    private static readonly int CacheSize = 2;

    public ObservableCollection<IMediaFlipViewItemModel> Items { get; private set; } = new ObservableCollection<IMediaFlipViewItemModel>();

    public IMediaFlipViewItemModel? SelectedItemModel { get; set; } // restore on back nav

    public bool ShowNoItemsUI { get; private set; } = true;

    public bool CanSelectPrevious { get; private set; } = false;

    public bool CanSelectNext { get; private set; } = false;

    public bool IsDiashowActive { get; private set; } = false;

    public bool IsDiashowLoopActive { get; private set; } = false;

    [DependsOn(nameof(SelectedItemModel))]
    public int SelectedItemNumber => SelectedItemModel is null ? 0 : Items.IndexOf(SelectedItemModel) + 1;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService loadMediaItemsService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly ApplicationSettings settings;

    private ICollection<IMediaFlipViewItemModel> loadedItemModels = Array.Empty<IMediaFlipViewItemModel>();

    private bool isSelectionChangedByDiashowLoop = false;

    private CancellationTokenSource? diashowLoopCancellationTokenSource;

    public MediaFlipViewModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory,
        ApplicationSettings settings) : base(messenger)
    {
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.mediaFlipViewItemModelFactory = mediaFlipViewItemModelFactory;
        this.settings = settings;

        Messenger.Register<MediaItemsLoadedMessage>(this, msg =>
        {
            Log.Debug("MediaItemsLoadedMessage received");
            SetItems(msg.MediaItems, msg.StartItem);
        });

        Messenger.Register<MediaItemsDeletedMessage>(this, msg =>
        {
            Items.RemoveAll(itemModel => msg.MediaItems.Contains(itemModel.MediaItem));
        });

        Messenger.Register<StartDiashowMessage>(this, msg =>
        {
            IsDiashowActive = true;
            IsDiashowLoopActive = true;
        });

        Messenger.Register<ExitDiashowMessage>(this, msg =>
        {
            IsDiashowActive = false;
            IsDiashowLoopActive = false;
        });
    }

    public void SetItems(IEnumerable<IMediaFileInfo> mediaItems, IMediaFileInfo? startItem = null)
    {
        Log.Debug("SetItems called");
        Stopwatch sw = Stopwatch.StartNew();
        Items = new ObservableCollection<IMediaFlipViewItemModel>(mediaItems.Select(mediaFlipViewItemModelFactory.Invoke));
        sw.Stop();
        Log.Info($"Create {Items.Count} item models took {sw.ElapsedMilliseconds} ms");
        Log.Debug("Set SelectedItemModel");
        SelectedItemModel = startItem != null ? Items.FirstOrDefault(itemModel => itemModel.MediaItem == startItem) : Items.FirstOrDefault();
    }

    public void Diashow_SelectPrevious()
    {
        Debug.Assert(IsDiashowActive);

        if (CanSelectPrevious)
        {
            SelectPrevious();
        }
        else
        {
            SelectedItemModel = Items.Last();
        }
    }

    public void Diashow_SelectNext()
    {
        Debug.Assert(IsDiashowActive);

        if (CanSelectNext)
        {
            SelectNext();
        }
        else
        {
            SelectedItemModel = Items.First();
        }
    }

    partial void OnItemsChanged()
    {
        ShowNoItemsUI = !Items.Any();
    }

    partial void OnSelectedItemModelChanged()
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

        if (!isSelectionChangedByDiashowLoop)
        {
            IsDiashowLoopActive = false;
        }
    }

    partial void OnIsDiashowLoopActiveChanged()
    {
        if (IsDiashowLoopActive)
        {
            EnableDiashowLoop();
        }
        else
        {
            DisableDiashowLoop();
        }
    }

    private void EnableDiashowLoop()
    {
        diashowLoopCancellationTokenSource?.Cancel();
        diashowLoopCancellationTokenSource = new CancellationTokenSource();

        async void loop(CancellationToken cancellationToken)
        {
            while (true)
            {
                await (SelectedItemModel!.PlaybackCompletedTask ?? Task.Delay(settings.DiashowTime));
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                isSelectionChangedByDiashowLoop = true;
                Diashow_SelectNext();
                isSelectionChangedByDiashowLoop = false;
            }
        }

        loop(diashowLoopCancellationTokenSource.Token);
    }

    private void DisableDiashowLoop()
    {
        diashowLoopCancellationTokenSource?.Cancel();
        diashowLoopCancellationTokenSource = null;
    }

    private void UpdateFlipViewItemModels()
    {
        ICollection<IMediaFlipViewItemModel> itemModelsToBeLoaded;

        if (SelectedItemModel is not null)
        {
            int selectedIndex = Items.IndexOf(SelectedItemModel);

            int startIndex = Math.Max(selectedIndex - CacheSize, 0);
            int endIndex = Math.Min(selectedIndex + CacheSize, Items.Count - 1);
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
            _ = itemModel.InitializeAsync();
        }

        loadedItemModels = itemModelsToBeLoaded;
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
    private void ToogleDiashowLoop()
    {
        if (IsDiashowLoopActive)
        {
            IsDiashowLoopActive = false;
            DisableDiashowLoop();
        }
        else
        {
            IsDiashowLoopActive = true;
            EnableDiashowLoop();
        }
    }

    [RelayCommand]
    private void ExitDiashow()
    {
        Messenger.Send(new ExitDiashowMessage());
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
            Messenger.Send(new MediaItemsLoadedMessage(result.MediaItems, result.StartItem));
        }
    }
}
