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
using Tocronx.SimpleAsync;

namespace PhotoViewer.App.ViewModels;

public interface IMediaFlipViewModel : IViewModel
{
    IMediaFileInfo? SelectedItem { get; }
    IMediaFlipViewItemModel? SelectedItemModel { get; }
    IRelayCommand SelectPreviousCommand { get; }
    IRelayCommand SelectNextCommand { get; }
    void SetItems(IEnumerable<IMediaFileInfo> mediaItems, IMediaFileInfo? startItem = null);
}

public partial class MediaFlipViewModel : ViewModelBase, IMediaFlipViewModel
{
    public event EventHandler<AsyncEventArgs>? DeleteAnimationRequested;

    private static readonly int CacheSize = 2;

    public ObservableCollection<IMediaFileInfo> Items { get; private set; } = new ObservableCollection<IMediaFileInfo>();

    public IReadOnlyCollection<IMediaFlipViewItemModel> ItemModels => loadedItemModels;

    public IMediaFileInfo? SelectedItem { get; private set; }

    public IMediaFlipViewItemModel? SelectedItemModel { get; private set; }

    public bool ShowNoItemsUI => !ShowLoadingUI && !Items.Any();

    public bool CanSelectPrevious => SelectedItem != null && Items.IndexOf(SelectedItem) > 0;

    public bool CanSelectNext => SelectedItem != null && Items.IndexOf(SelectedItem) < Items.Count - 1;

    public bool IsDiashowActive { get; private set; } = false;

    public bool IsDiashowLoopActive { get; private set; } = false;

    public int SelectedItemNumber => SelectedItem is null ? 0 : Items.IndexOf(SelectedItem) + 1;

    public bool ShowSelectedItemIndicator => Items.Any() && !IsDiashowLoopActive;

    public bool ShowLoadingUI { get; private set; }

    public bool IsLoadingMoreFiles { get; private set; }

    public bool IsNotLoadingMoreFiles => !IsLoadingMoreFiles;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly ApplicationSettings settings;

    private readonly HashSet<IMediaFlipViewItemModel> loadedItemModels = new HashSet<IMediaFlipViewItemModel>();

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
            bool preview = false;

            if (msg.LoadMediaFilesTask.StartMediaFile is { } startFile)
            {
                preview = true;
                SetItems(new[] { startFile }, startFile);
                ShowLoadingUI = false;
                IsLoadingMoreFiles = true;
            }

            var result = await msg.LoadMediaFilesTask.WaitForResultAsync(); // TODO error handling

            SetItems(result.MediaFiles, result.StartMediaFile);

            if (preview)
            {
                // linked files may have changed -> update window title
                Messenger.Send(new ChangeWindowTitleMessage(SelectedItem?.DisplayName ?? ""));

                UpdateFlipViewItemModels();
            }

            ShowLoadingUI = false;
            IsLoadingMoreFiles = false;
        });

        Messenger.Register<MediaFilesDeletedMessage>(this, async msg =>
        {
            if (settings.ShowDeleteAnimation && msg.Files.Contains(SelectedItem)) 
            {
                var asyncEventArgs = new AsyncEventArgs();
                DeleteAnimationRequested?.Invoke(this, asyncEventArgs);
                await asyncEventArgs.CompletionTask;
            }
            var selectedIndex = Items.IndexOf(SelectedItem!);
            msg.Files.ForEach(file => Items.Remove(file));            
            SelectedItem = Items.ElementAtOrDefault(Math.Min(selectedIndex, Items.Count - 1));
            UpdateFlipViewItemModels();
        });

        Messenger.Register<StartDiashowMessage>(this, msg =>
        {
            IsDiashowActive = true;
            ItemModels.ForEach(itemModel => itemModel.IsDiashowActive = true);
            IsDiashowLoopActive = true;
        });

        Messenger.Register<ExitDiashowMessage>(this, msg =>
        {
            IsDiashowActive = false;
            ItemModels.ForEach(itemModel => itemModel.IsDiashowActive = false);
            IsDiashowLoopActive = false;
        });
    }

    protected override void OnCleanup()
    {
        loadedItemModels.ForEach(itemModel => itemModel.Cleanup());
        diashowLoopCancellationTokenSource?.Cancel();
    }

    public void SetItems(IEnumerable<IMediaFileInfo> files, IMediaFileInfo? startFile = null)
    {
        Log.Debug("SetItems called");
        ignoreSelectionChanges = true;
        Stopwatch sw = Stopwatch.StartNew();
        Items = new ObservableCollection<IMediaFileInfo>(files);
        sw.Stop();
        Log.Info($"Set {Items.Count} Items took {sw.ElapsedMilliseconds} ms");  
        ignoreSelectionChanges = false;
        var fileToSelect = startFile ?? Items.FirstOrDefault();
        Log.Debug("Set SelectedItem " + fileToSelect);
        SelectedItem = fileToSelect;        
    }

    public IMediaFlipViewItemModel? TryGetItemModel(IMediaFileInfo mediaFile)
    {
        return loadedItemModels.FirstOrDefault(itemModel => itemModel.MediaItem == mediaFile);
    }

    public void Select(IMediaFileInfo? mediaFileInfo)
    {
        if (ignoreSelectionChanges) 
        {
            throw new InvalidOperationException();
        }
        if (mediaFileInfo is null && Items.Any())
        {
            throw new InvalidOperationException();
        }
        Log.Debug("Set SelectedItem " + mediaFileInfo);
        SelectedItem = mediaFileInfo;
    }

    partial void OnSelectedItemChanged()
    {
        Log.Info($"Selection changed to {SelectedItem?.DisplayName ?? "null"}");

        Messenger.Send(new ChangeWindowTitleMessage(SelectedItem?.DisplayName ?? ""));

        UpdateFlipViewItemModels();

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
            while (SelectedItemModel is not null)
            {
                await (SelectedItemModel.PlaybackCompletedTask ?? Task.Delay(settings.DiashowTime));
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                isSelectionChangedByDiashowLoop = true;
                SelectNext();
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
        Stopwatch sw = Stopwatch.StartNew();

        ICollection<IMediaFileInfo> itemsToBeLoaded;

        if (SelectedItem is not null)
        {
            int selectedIndex = Items.IndexOf(SelectedItem);

            int startIndex = Math.Max(selectedIndex - CacheSize, 0);
            int endIndex = Math.Min(selectedIndex + CacheSize, Items.Count - 1);
            itemsToBeLoaded = Items.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        }
        else
        {
            itemsToBeLoaded = Array.Empty<IMediaFileInfo>();
        }

        var itemsToLoad = itemsToBeLoaded.Except(loadedItemModels.Select(itemModel => itemModel.MediaItem)).ToList();
        var itemModlesToCleanup = loadedItemModels.Where(itemModel => !itemsToBeLoaded.Contains(itemModel.MediaItem)).ToList();

        foreach (var mediaFile in itemsToLoad)
        {
            Log.Info($"Initialize ViewModel for {mediaFile.DisplayName}");
            var itemModel = mediaFlipViewItemModelFactory.Invoke(mediaFile);
            loadedItemModels.Add(itemModel);
            itemModel.IsDiashowActive = IsDiashowActive;
            itemModel.InitializeAsync().FireAndForget();
        }

        foreach (var itemModel in itemModlesToCleanup)
        {
            Log.Info($"Cleanup ViewModel for {itemModel.MediaItem.DisplayName}");
            itemModel.Cleanup();
            loadedItemModels.Remove(itemModel);
        }

        SelectedItemModel = SelectedItem != null ? TryGetItemModel(SelectedItem) : null;
        ItemModels.ForEach(itemModel => itemModel.IsSelected = itemModel == SelectedItemModel);

        sw.Stop();
        Log.Info($"UpdateFlipViewItemModels took {sw.ElapsedMilliseconds} ms"); ;
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevious))]
    public void SelectPrevious()
    {
        if (CanSelectPrevious)
        {
            SelectedItem = Items[Items.IndexOf(SelectedItem!) - 1];
        }
        else if(IsDiashowActive && Items.Any()) 
        {
            SelectedItem = Items.Last();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectNext))]
    public void SelectNext()
    {
        if (CanSelectNext)
        {
            SelectedItem = Items[Items.IndexOf(SelectedItem!) + 1];
        }
        else if (IsDiashowActive && Items.Any())
        {
            SelectedItem = Items.First();
        }
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
            var loadMediaFilesTask = mediaFilesLoaderService.LoadFolder(folder, config);
            Messenger.Send(new MediaFilesLoadingMessage(loadMediaFilesTask));
        }
    }
}
