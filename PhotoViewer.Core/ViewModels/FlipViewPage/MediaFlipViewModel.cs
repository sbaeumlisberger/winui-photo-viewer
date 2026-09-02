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
using PhotoViewer.Core.ViewModels.Shared;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.Storage;
using Windows.System;

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

    public partial ObservableList<IMediaFileInfo> Items { get; private set; } = new ObservableList<IMediaFileInfo>();

    public IReadOnlyCollection<IMediaFlipViewItemModel> ItemModels => itemModelsCache.Values;

    public partial IMediaFileInfo? SelectedItem { get; private set; }

    public partial IMediaFlipViewItemModel? SelectedItemModel { get; private set; }

    public partial int SelectedIndex { get; private set; } = -1;

    public bool ShowNoItemsUI => !ShowLoadingUI && SelectedIndex == -1;

    public bool CanSelectPrevious => SelectedIndex > 0;

    public bool CanSelectNext => SelectedIndex < Items.Count - 1;

    public partial bool IsSlideshowActive { get; private set; } = false;

    public partial bool IsSlideshowLoopActive { get; private set; } = false;

    public int SelectedItemNumber => SelectedIndex + 1;

    public bool ShowSelectedItemIndicator => SelectedIndex != -1 && !IsSlideshowActive;

    public partial bool ShowLoadingUI { get; private set; }

    public partial bool IsLoadingMoreFiles { get; private set; }

    public bool IsNotLoadingMoreFiles => !IsLoadingMoreFiles;

    public InfoBarModel InfoBarModel { get; } = new InfoBarModel();

    public KeyboardAcceleratorViewModel RestoreLastDeletedFileKeyboardAccelerator { get; } = new(VirtualKeyModifiers.Control, VirtualKey.Z);

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly IFileSystemService fileSystemService;

    private readonly ApplicationSettings settings;

    private bool isSelectionChangedBySlideshowLoop = false;

    private CancellationTokenSource? slideshowLoopCancellationTokenSource;

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

        Register<StartSlideshowMessage>(msg =>
        {
            IsSlideshowActive = true;
            ItemModels.ForEach(itemModel => itemModel.IsSlideshowActive = true);
            IsSlideshowLoopActive = true;
        });

        Register<ExitSlideshowMessage>(msg =>
        {
            IsSlideshowActive = false;
            ItemModels.ForEach(itemModel => itemModel.IsSlideshowActive = false);
            IsSlideshowLoopActive = false;
        });

        Register<MediaFilesRenamedMessage>(OnReceiveMediaFilesRenamedMessage);

        Register<FilesSortedMessage>(msg => SetFiles(msg.SortedFiles, SelectedItem));
    }

    protected override void OnCleanup()
    {
        itemModelsCache.ClearCache();
        slideshowLoopCancellationTokenSource?.Cancel();
    }

    private async void OnReceiveMediaFilesLoadingMessage(MediaFilesLoadingMessage msg)
    {
        try
        {
            ShowLoadingUI = true;

            SetFiles(Array.Empty<IMediaFileInfo>());
            lastDeletedFileInfo = null;

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
            InfoBarModel.ShowMessage(new InfoBarModel.InfoBarMessage()
            {
                Text = string.Format(Strings.FileDeletedMessage, SelectedItem.DisplayName),
                Command = RestoreLastDeletedFileCommand,
                CommandLabel = Strings.RestoreDeletedFileLabel,
                CommandKeyboardAccelerator = RestoreLastDeletedFileKeyboardAccelerator
            });
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

        if (!isSelectionChangedBySlideshowLoop)
        {
            IsSlideshowLoopActive = false;
        }
    }

    private void UpdateSelectedIndex()
    {
        SelectedIndex = SelectedItem != null ? Items.IndexOf(SelectedItem) : -1;
    }

    partial void OnIsSlideshowLoopActiveChanged()
    {
        if (IsSlideshowLoopActive)
        {
            EnableSlideshowLoop();
        }
        else
        {
            DisableSlideshowLoop();
        }
    }

    private void EnableSlideshowLoop()
    {
        slideshowLoopCancellationTokenSource?.Cancel();
        slideshowLoopCancellationTokenSource = new CancellationTokenSource();

        async void loop(CancellationToken cancellationToken)
        {
            while (SelectedItemModel is not null)
            {
                await (SelectedItemModel.SlideshowTask ?? Task.Delay(settings.SlideshowTime));
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                isSelectionChangedBySlideshowLoop = true;
                SelectNext();
                isSelectionChangedBySlideshowLoop = false;
            }
        }

        loop(slideshowLoopCancellationTokenSource.Token);
    }

    private void DisableSlideshowLoop()
    {
        slideshowLoopCancellationTokenSource?.Cancel();
        slideshowLoopCancellationTokenSource = null;
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
        itemModel.IsSlideshowActive = IsSlideshowActive;
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
        else if (IsSlideshowActive && Items.Any())
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
        else if (IsSlideshowActive && Items.Any())
        {
            SelectedItem = Items.First();
        }
    }

    [RelayCommand(CanExecute = nameof(IsSlideshowActive))]
    private void ToggleSlideshowLoop()
    {
        IsSlideshowLoopActive = !IsSlideshowLoopActive;
    }

    [RelayCommand(CanExecute = nameof(IsSlideshowActive))]
    private void ExitSlideshow()
    {
        Messenger.Send(new ExitSlideshowMessage());
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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RestoreLastDeletedFileAsync()
    {
        if (this.lastDeletedFileInfo is null)
        {
            return;
        }

        var lastDeletedFileInfo = this.lastDeletedFileInfo.Value;

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

            if (SelectedItem is null)
            {
                UpdateFlipViewItemModels(lastDeletedFileInfo.File);
                SelectedItem = lastDeletedFileInfo.File;
            }

            Messenger.Send(new MediaFileRestoredMessage(lastDeletedFileInfo.File, lastDeletedFileInfo.Index));

            InfoBarModel.ShowMessage(string.Format(Strings.FileRestoredMessage, lastDeletedFileInfo.File.DisplayName));

            this.lastDeletedFileInfo = null;
        }
        catch (Exception e)
        {
            Log.Error("Failed to restore file", e);
            InfoBarModel.ShowMessage(string.Format(Strings.RestoreFileFailedMessage, lastDeletedFileInfo.File.DisplayName), InfoBarSeverity.Error);
        }
    }

}
