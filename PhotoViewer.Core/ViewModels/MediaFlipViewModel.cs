using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using PhotoViewer.Core.Utils;
using Microsoft.Graphics.Canvas.Text;
using PhotoViewer.Core.Messages;

namespace PhotoViewer.App.ViewModels;

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

    public IMediaFlipViewItemModel? SelectedItemModel { get; set; } // TODO restore on back nav

    public bool ShowNoItemsUI => !ShowLoadingUI && !Items.Any();

    public bool CanSelectPrevious { get; private set; } = false;

    public bool CanSelectNext { get; private set; } = false;

    public bool IsDiashowActive { get; private set; } = false;

    public bool IsDiashowLoopActive { get; private set; } = false;

    public int SelectedItemNumber => SelectedItemModel is null ? 0 : Items.IndexOf(SelectedItemModel) + 1;

    public bool ShowSelectedItemIndicator => Items.Any() && !IsDiashowLoopActive;

    public bool ShowLoadingUI { get; private set; }

    public bool IsLoadingMoreFiles { get; private set; }

    public bool IsNotLoadingMoreFiles => !IsLoadingMoreFiles;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly ApplicationSettings settings;

    private ICollection<IMediaFlipViewItemModel> loadedItemModels = Array.Empty<IMediaFlipViewItemModel>();

    private bool isSelectionChangedByDiashowLoop = false;

    private CancellationTokenSource? diashowLoopCancellationTokenSource;

    private bool ignoreSelectionChanges = false;

    public MediaFlipViewModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService loadMediaItemsService,
        Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory,
        ApplicationSettings settings) : base(messenger)
    {
        this.dialogService = dialogService;
        this.mediaFilesLoaderService = loadMediaItemsService;
        this.mediaFlipViewItemModelFactory = mediaFlipViewItemModelFactory;
        this.settings = settings;

        Messenger.Register<MediaFilesLoadingMessage>(this, async msg =>
        {
            ShowLoadingUI = true;
           
            if (msg.LoadMediaFilesTask.StartMediaFile is { } startFile) 
            {               
                SetItems(new[] { startFile }, startFile);
                ShowLoadingUI = false;
                IsLoadingMoreFiles = true;
            }
   
            var result = await msg.LoadMediaFilesTask.WaitForResultAsync(); // TODO error handling
            SetItems(result.MediaFiles, result.StartMediaFile);
            ShowLoadingUI = false;
            IsLoadingMoreFiles = false;
        });

        Messenger.Register<MediaFilesDeletedMessage>(this, msg =>
        {
            Items.RemoveAll(itemModel => msg.Files.Contains(itemModel.MediaItem));
        });

        Messenger.Register<StartDiashowMessage>(this, msg =>
        {
            IsDiashowActive = true;
            Items.ForEach(itemModel => itemModel.IsDiashowActive = true);
            IsDiashowLoopActive = true;
        });

        Messenger.Register<ExitDiashowMessage>(this, msg =>
        {
            IsDiashowActive = false;
            Items.ForEach(itemModel => itemModel.IsDiashowActive = false);
            IsDiashowLoopActive = false;
        });
    }

    protected override void OnViewDisconnectedOverride()
    {
        diashowLoopCancellationTokenSource?.Cancel();
    }

    public void SetItems(IEnumerable<IMediaFileInfo> files, IMediaFileInfo? startFile = null)
    {
        Log.Debug("SetItems called");
        ignoreSelectionChanges = true;
        Stopwatch sw = Stopwatch.StartNew();
        Items = new ObservableCollection<IMediaFlipViewItemModel>(files.Select(file => 
        {
            if (file.Equals(SelectedItemModel?.MediaItem)) 
            {
                // linked files may have been changed
                Messenger.Send(new ChangeWindowTitleMessage(SelectedItemModel.MediaItem.DisplayName ?? ""));
                
                // reuse model from startup
                return SelectedItemModel;
            }
            return mediaFlipViewItemModelFactory.Invoke(file);
        }));
        sw.Stop();
        Log.Info($"Create {Items.Count} item models took {sw.ElapsedMilliseconds} ms");
        Log.Debug("Set SelectedItemModel");
        SelectedItemModel = null;
        ignoreSelectionChanges = false;
        SelectedItemModel = startFile != null ? Items.FirstOrDefault(itemModel => itemModel.MediaItem.Equals(startFile)) : Items.FirstOrDefault();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (ignoreSelectionChanges && e.PropertyName == nameof(SelectedItemModel))
        {
            return;
        }
        base.OnPropertyChanged(e);
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

    partial void OnSelectedItemModelChanged()
    {
        Log.Info($"Selection changed to {SelectedItemModel?.MediaItem.DisplayName}");

        Messenger.Send(new ChangeWindowTitleMessage(SelectedItemModel?.MediaItem.DisplayName ?? ""));

        UpdateFlipViewItemModels();

        CanSelectPrevious = SelectedItemModel != null && Items.IndexOf(SelectedItemModel) > 0;
        CanSelectNext = SelectedItemModel != null && Items.IndexOf(SelectedItemModel) < Items.Count - 1;

        loadedItemModels.ForEach(itemModel => itemModel.IsSelected = itemModel == SelectedItemModel);
        
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

    private async void UpdateFlipViewItemModels()
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

        var itemsToLoad = itemModelsToBeLoaded.Except(loadedItemModels).ToList();
        var itemModlesToCleanup = loadedItemModels.Except(itemModelsToBeLoaded).ToList();

        var selectedItemModel = SelectedItemModel;

        if (SelectedItemModel is not null && itemsToLoad.Remove(SelectedItemModel)) 
        {
            await SelectedItemModel.PrepareAsync();
        }

        if (SelectedItemModel != selectedItemModel) 
        {
            return;
        }

        foreach (var itemModel in itemsToLoad)
        {
            Log.Info($"Prepare ViewModel for {itemModel.MediaItem.DisplayName}");
            _ = itemModel.PrepareAsync();
        }

        loadedItemModels = itemModelsToBeLoaded;

        foreach (var itemModel in itemModlesToCleanup)
        {
            Log.Info($"Cleanup ViewModel for {itemModel.MediaItem.DisplayName}");
            Debug.Assert(!loadedItemModels.Contains(itemModel));
            itemModel.Cleanup();
        }
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
            var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);
            var loadMediaFilesTask = mediaFilesLoaderService.LoadMediaFilesFromFolder(folder, config);
            Messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
        }
    }
}
