﻿using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty]
    private bool isDiashowActive = false;

    [ObservableProperty]
    private bool isDiashowLoopActive = false;

    private readonly IMessenger messenger;

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
            IsDiashowActive = true;
            IsDiashowLoopActive = true;
        });

        messenger.Subscribe<ExitDiashowMessage>(msg =>
        {
            IsDiashowActive = false;
            IsDiashowLoopActive = false;
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

    public void Diashow_SelectPrevious()
    {
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
        if (CanSelectNext)
        {
            SelectNext();
        }
        else
        {
            SelectedItemModel = Items.First();
        }
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

        if (!isSelectionChangedByDiashowLoop)
        {
            IsDiashowLoopActive = false;
        }
    }

    partial void OnIsDiashowLoopActiveChanged(bool value)
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
            await Task.Delay(settings.DiashowTime);
            while (!cancellationToken.IsCancellationRequested)
            {
                isSelectionChangedByDiashowLoop = true;
                Diashow_SelectNext();
                isSelectionChangedByDiashowLoop = false;
                await Task.Delay(settings.DiashowTime);
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
        messenger.Publish(new ExitDiashowMessage());
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
