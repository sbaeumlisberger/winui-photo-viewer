﻿using CommunityToolkit.Mvvm.Input;
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
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core;

namespace PhotoViewer.App.ViewModels;

public interface IMediaFlipViewModel : IViewModel
{
    IMediaFileInfo? SelectedItem { get; }
    IMediaFlipViewItemModel? SelectedItemModel { get; }
    IRelayCommand SelectPreviousCommand { get; }
    IRelayCommand SelectNextCommand { get; }
    void SetItems(IReadOnlyList<IMediaFileInfo> mediaItems, IMediaFileInfo? startItem = null);
}

public partial class MediaFlipViewModel : ViewModelBase, IMediaFlipViewModel
{
    public event EventHandler<AsyncEventArgs>? DeleteAnimationRequested;

    private static readonly int CacheSize = 2;

    public ObservableList<IMediaFileInfo> Items { get; private set; } = new ObservableList<IMediaFileInfo>();

    public IReadOnlyCollection<IMediaFlipViewItemModel> ItemModels => itemModels.RealizedValues;

    public IMediaFileInfo? SelectedItem { get; private set; }

    public IMediaFlipViewItemModel? SelectedItemModel { get; private set; }

    public bool ShowNoItemsUI => !ShowLoadingUI && !Items.Any();

    public bool CanSelectPrevious => SelectedItem != null && Items.IndexOf(SelectedItem) > 0;

    public bool CanSelectNext => SelectedItem != null && Items.IndexOf(SelectedItem) < Items.Count - 1;

    public bool IsDiashowActive { get; private set; } = false;

    public bool IsDiashowLoopActive { get; private set; } = false;

    public int SelectedItemNumber => SelectedItem is null ? 0 : Items.IndexOf(SelectedItem) + 1;

    public bool ShowSelectedItemIndicator => Items.Any() && !IsDiashowActive;

    public bool ShowLoadingUI { get; private set; }

    public bool IsLoadingMoreFiles { get; private set; }

    public bool IsNotLoadingMoreFiles => !IsLoadingMoreFiles;

    private readonly IDialogService dialogService;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    private readonly ApplicationSettings settings;

    private bool isSelectionChangedByDiashowLoop = false;

    private CancellationTokenSource? diashowLoopCancellationTokenSource;

    private VirtualizedCollection<IMediaFileInfo, IMediaFlipViewItemModel> itemModels;

    internal MediaFlipViewModel(
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

        itemModels = new(Array.Empty<IMediaFileInfo>(), CacheSize, CreateItemModel, CleanupItemModel);

        Register<MediaFilesLoadingMessage>(OnReceive);

        Register<MediaFilesDeletedMessage>(OnReceive);

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

        Register<MediaFilesRenamedMessage>(OnReceive);
    }

    protected override void OnCleanup()
    {
        itemModels.Clear();
        diashowLoopCancellationTokenSource?.Cancel();
    }

    private async void OnReceive(MediaFilesLoadingMessage msg)
    {          
        try
        {
            ShowLoadingUI = true;

            SetItems(Array.Empty<IMediaFileInfo>());

            bool preview = false;

            if (msg.LoadMediaFilesTask.PreviewMediaFile is { } previewMediaFile)
            {
                preview = true;
                SetItems(new[] { previewMediaFile }, previewMediaFile);
                ShowLoadingUI = false;
                IsLoadingMoreFiles = true;
            }

            var result = await msg.LoadMediaFilesTask.WaitForResultAsync();

            SetItems(result.MediaFiles, result.StartMediaFile);

            if (preview)
            {
                // update window title because linked files may have changed
                Messenger.Send(new ChangeWindowTitleMessage(SelectedItem?.DisplayName ?? ""));

                // trigger creation of the item models of the loaded files
                UpdateFlipViewItemModels(); 
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

    private async void OnReceive(MediaFilesDeletedMessage msg)
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
    }

    private void OnReceive(MediaFilesRenamedMessage message)
    {
        if (SelectedItem is not null && message.MediaFiles.Contains(SelectedItem))
        {
            Messenger.Send(new ChangeWindowTitleMessage(SelectedItem.DisplayName ?? ""));
        }
    }

    public void SetItems(IReadOnlyList<IMediaFileInfo> files, IMediaFileInfo? startFile = null)
    {
        Log.Debug("SetItems called");
        var items = new ObservableList<IMediaFileInfo>(files);
        var selectedItem = startFile ?? items.FirstOrDefault();
        itemModels.SetKeys(items);
        itemModels.SetSelectedItem(selectedItem);
        Items = items;    
        SelectedItem = selectedItem;
    }

    public IMediaFlipViewItemModel? TryGetItemModel(IMediaFileInfo mediaFile)
    {
        return ItemModels.FirstOrDefault(itemModel => itemModel.MediaItem == mediaFile);
    }

    public void Select(IMediaFileInfo? mediaFileInfo)
    {
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
        SelectedItemModel = itemModels.SetSelectedItem(SelectedItem);
        ItemModels.ForEach(itemModel => itemModel.IsSelected = itemModel == SelectedItemModel);
        sw.Stop();
        Log.Info($"UpdateFlipViewItemModels took {sw.ElapsedMilliseconds} ms"); ;
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
        Log.Info($"Cleanup ViewModel for {itemModel.MediaItem.DisplayName}");
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
}
