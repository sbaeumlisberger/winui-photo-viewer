using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public interface IMediaFlipViewModel : IViewModel
{
    IMediaFileInfo? SelectedItem { get; }
    IMediaFlipViewItemModel? SelectedItemModel { get; }
    IRelayCommand SelectPreviousCommand { get; }
    IRelayCommand SelectNextCommand { get; }
    void SetFiles(IReadOnlyList<IMediaFileInfo> mediaFiles, IMediaFileInfo? startFile = null);
}

public partial class MediaFlipViewModel : ViewModelBase, IMediaFlipViewModel
{
    public event EventHandler<AsyncEventArgs>? DeleteAnimationRequested;

    private static readonly int CacheSize = 2;

    public ObservableList<IMediaFileInfo> Items { get; private set; } = new ObservableList<IMediaFileInfo>();

    public IReadOnlyCollection<IMediaFlipViewItemModel> ItemModels => itemModelsCache.Values;

    public IMediaFileInfo? SelectedItem { get; private set; }

    public IMediaFlipViewItemModel? SelectedItemModel { get; private set; }

    public int SelectedIndex { get; private set; } = -1;

    public bool ShowNoItemsUI => !ShowLoadingUI && SelectedIndex == -1;

    public bool CanSelectPrevious => SelectedIndex > 0;

    public bool CanSelectNext => SelectedIndex < Items.Count - 1;

    public bool IsDiashowActive { get; private set; } = false;

    public bool IsDiashowLoopActive { get; private set; } = false;

    public int SelectedItemNumber => SelectedIndex + 1;

    public bool ShowSelectedItemIndicator => SelectedIndex != -1 && !IsDiashowActive;

    public bool ShowLoadingUI { get; private set; }

    public bool IsLoadingMoreFiles { get; private set; }

    public bool IsNotLoadingMoreFiles => !IsLoadingMoreFiles;

    public InfoBarModel InfoBarModel { get; } = new InfoBarModel();

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly IFileSystemService fileSystemService;

    private readonly ApplicationSettings settings;

    private bool isSelectionChangedByDiashowLoop = false;

    private CancellationTokenSource? diashowLoopCancellationTokenSource;

    private readonly VirtualizedCollection<IMediaFileInfo, IMediaFlipViewItemModel> itemModelsCache;

    private (IMediaFileInfo File, int Index)? lastDeletedFileInfo;

    internal MediaFlipViewModel(
        IMessenger messenger,
        IDialogService dialogService,
        IMediaFilesLoaderService mediaFilesLoaderService,
        IFileSystemService fileSystemService,
        Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory,
        ApplicationSettings settings) : base(messenger)
    {
        this.dialogService = dialogService;
        this.mediaFilesLoaderService = mediaFilesLoaderService;
        this.fileSystemService = fileSystemService;
        this.mediaFlipViewItemModelFactory = mediaFlipViewItemModelFactory;
        this.settings = settings;

        itemModelsCache = VirtualizedCollection.Create(CacheSize, CreateItemModel, CleanupItemModel, new ObservableList<IMediaFileInfo>());

        Register<MediaFilesLoadingMessage>(OnReceiveMediaFilesLoadingMessage);

        Register<MediaFilesDeletedMessage>(OnReceiveMediaFilesDeletedMessage);

        Register<StartDiashowMessage>(msg =>
        {
            IsDiashowActive = true;
            ItemModels.ForEach(itemModel => itemModel.IsDiashowActive = true);
            IsDiashowLoopActive = true;
        });

        Register<ExitDiashowMessage>(msg =>
        {
            IsDiashowActive = false;
            ItemModels.ForEach(itemModel => itemModel.IsDiashowActive = false);
            IsDiashowLoopActive = false;
        });

        Register<MediaFilesRenamedMessage>(OnReceiveMediaFilesRenamedMessage);

        Register<FilesSortedMessage>(msg => SetFiles(msg.SortedFiles, SelectedItem));
    }

    protected override void OnCleanup()
    {
        itemModelsCache.ClearCache();
        diashowLoopCancellationTokenSource?.Cancel();
    }

    private async void OnReceiveMediaFilesLoadingMessage(MediaFilesLoadingMessage msg)
    {
        try
        {
            ShowLoadingUI = true;

            SetFiles(Array.Empty<IMediaFileInfo>());

            bool preview = false;

            if (msg.LoadMediaFilesTask.PreviewMediaFile is { } previewMediaFile)
            {
                preview = true;
                SetFiles(new[] { previewMediaFile }, previewMediaFile);
                ShowLoadingUI = false;
                IsLoadingMoreFiles = true;
            }

            var result = await msg.LoadMediaFilesTask.WaitForResultAsync();

            SetFiles(result.MediaFiles, result.StartMediaFile);

            if (preview)
            {
                // update window title because linked files may have changed
                Messenger.Send(new ChangeWindowTitleMessage(SelectedItem?.DisplayName ?? ""));
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load files", ex);

            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.LoadFilesErrorDialog_Title,
                Message = Strings.LoadFilesErrorDialog_Message,
            });
        }
        finally
        {
            ShowLoadingUI = false;
            IsLoadingMoreFiles = false;
        }
    }

    private async void OnReceiveMediaFilesDeletedMessage(MediaFilesDeletedMessage msg)
    {
        if (SelectedItem is not null && msg.Files.Count == 1 && msg.Files.Single() == SelectedItem)
        {
            InfoBarModel.ShowMessage(string.Format(Strings.FileDeletedMessage, SelectedItem.DisplayName),
                command: RestoreLastDeletedFileCommand, commandLabel: Strings.RestoreDeletedFileLabel);
            lastDeletedFileInfo = (SelectedItem, SelectedIndex);
        }

        if (settings.ShowDeleteAnimation && msg.Files.Contains(SelectedItem))
        {
            var asyncEventArgs = new AsyncEventArgs();
            DeleteAnimationRequested?.Invoke(this, asyncEventArgs);
            await asyncEventArgs.CompletionTask;
        }

        var selectedIndex = Items.IndexOf(SelectedItem!);
        var newItems = Items.Except(msg.Files).ToList();
        // change selection first to avoid reset of flip view control
        Log.Debug("MediaFilesDeletedMessage -> Set SelectedItem");
        SelectedItem = newItems.ElementAtOrDefault(Math.Min(selectedIndex, newItems.Count - 1));
        Log.Debug("MediaFilesDeletedMessage -> Update Items");
        Items.RemoveRange(msg.Files);
        UpdateFlipViewItemModels(SelectedItem);
    }

    private void OnReceiveMediaFilesRenamedMessage(MediaFilesRenamedMessage message)
    {
        if (SelectedItem is not null && message.MediaFiles.Contains(SelectedItem))
        {
            Messenger.Send(new ChangeWindowTitleMessage(SelectedItem.DisplayName ?? ""));
        }
    }

    public void SetFiles(IReadOnlyList<IMediaFileInfo> files, IMediaFileInfo? startFile = null)
    {
        Log.Debug($"SetItems called with {files.Count} files and {startFile} as start file");
        var items = new ObservableList<IMediaFileInfo>(files);
        var selectedItem = startFile ?? items.FirstOrDefault();
        itemModelsCache.SetKeys(items);
        UpdateFlipViewItemModels(selectedItem);
        Items.CollectionChanged -= Items_CollectionChanged;
        Items = items;
        Items.CollectionChanged += Items_CollectionChanged;
        SelectedItem = selectedItem;
        UpdateSelectedIndex();
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSelectedIndex();
    }

    public IMediaFlipViewItemModel? TryGetItemModel(IMediaFileInfo mediaFile)
    {
        return ItemModels.FirstOrDefault(itemModel => itemModel.MediaFile == mediaFile);
    }

    public void Select(IMediaFileInfo? mediaFileInfo)
    {
        if (mediaFileInfo is null && Items.Any())
        {
            throw new InvalidOperationException();
        }
        Log.Debug($"Set {mediaFileInfo} as SelectedItem");
        SelectedItem = mediaFileInfo;
    }

    partial void OnSelectedItemChanged()
    {
        Log.Info($"Selection changed to {SelectedItem?.DisplayName}");

        UpdateSelectedIndex();

        Messenger.Send(new ChangeWindowTitleMessage(SelectedItem?.DisplayName ?? ""));

        UpdateFlipViewItemModels(SelectedItem);

        if (!isSelectionChangedByDiashowLoop)
        {
            IsDiashowLoopActive = false;
        }
    }

    private void UpdateSelectedIndex()
    {
        SelectedIndex = SelectedItem != null ? Items.IndexOf(SelectedItem) : -1;
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

    private void UpdateFlipViewItemModels(IMediaFileInfo? selectedItem)
    {
        Stopwatch sw = Stopwatch.StartNew();
        SelectedItemModel = itemModelsCache.SetSelectedItem(selectedItem);
        ItemModels.ForEach(itemModel => itemModel.IsSelected = itemModel == SelectedItemModel);
        sw.Stop();
        Log.Debug($"UpdateFlipViewItemModels took {sw.ElapsedMilliseconds} ms"); ;
    }

    private IMediaFlipViewItemModel CreateItemModel(IMediaFileInfo mediaFile)
    {
        Log.Info($"Initialize ViewModel for {mediaFile.DisplayName}");
        var itemModel = mediaFlipViewItemModelFactory.Invoke(mediaFile);
        itemModel.IsDiashowActive = IsDiashowActive;
        itemModel.InitializeAsync().LogOnException();
        return itemModel;
    }

    private void CleanupItemModel(IMediaFlipViewItemModel itemModel)
    {
        Log.Info($"Cleanup ViewModel for {itemModel.MediaFile.DisplayName}");
        itemModel.IsSelected = false;
        itemModel.Cleanup();
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevious))]
    public void SelectPrevious()
    {
        if (CanSelectPrevious)
        {
            SelectedItem = Items[Items.IndexOf(SelectedItem!) - 1];
        }
        else if (IsDiashowActive && Items.Any())
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

    [RelayCommand(CanExecute = nameof(IsDiashowActive))]
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

    [RelayCommand(CanExecute = nameof(IsDiashowActive))]
    private void ExitDiashow()
    {
        Messenger.Send(new ExitDiashowMessage());
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
    private async Task RestoreLastDeletedFileAsync()
    {
        var lastDeletedFileInfo = this.lastDeletedFileInfo!.Value;

        try
        {
            InfoBarModel.HideMessage();

            await Task.Run(() => 
            { 
                foreach (var storageFile in lastDeletedFileInfo.File.StorageFiles)
                {
                    fileSystemService.Restore(storageFile);
                }
            });

            Items.Insert(lastDeletedFileInfo.Index, lastDeletedFileInfo.File);

            Messenger.Send(new MediaFileRestoredMessage(lastDeletedFileInfo.File, lastDeletedFileInfo.Index));

            InfoBarModel.ShowMessage($"File {lastDeletedFileInfo.File.DisplayName} restored");
        }
        catch (Exception e)
        {
            Log.Error("Failed to restore file", e);
            InfoBarModel.ShowMessage($"Could not restore {lastDeletedFileInfo.File.DisplayName}", InfoBarSeverity.Error);
        }
    }

}
