using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public partial class FlipViewPageModel : ViewModelBase
{
    public IDetailsBarModel DetailsBarModel { get; }

    public IFlipViewPageCommandBarModel CommandBarModel { get; }

    [ObservableProperty]
    private ObservableCollection<IMediaItem> items = new ObservableCollection<IMediaItem>();

    [ObservableProperty]
    private IMediaItem? selectedItem;

    [ObservableProperty]
    private bool showNoItemsUI = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectPreviousCommand))]
    private bool canSelectPrevious = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectNextCommand))]
    private bool canSelectNext = false;

    private readonly Dictionary<IMediaItem, IMediaFlipViewItemModel> flipViewItemModels = new Dictionary<IMediaItem, IMediaFlipViewItemModel>();

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly ILoadMediaItemsService loadMediaItemsService;

    private readonly Func<IMediaItem, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory;

    public FlipViewPageModel(
        IMessenger messenger,
        IDialogService dialogService,
        ILoadMediaItemsService loadMediaItemsService,
        Func<IDetailsBarModel> detailsBarModelFactory,
        Func<ICommand, ICommand, IFlipViewPageCommandBarModel> flipViewPageCommandBarModelFactory,
        Func<IMediaItem, IMediaFlipViewItemModel> mediaFlipViewItemModelFactory)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.loadMediaItemsService = loadMediaItemsService;
        this.mediaFlipViewItemModelFactory = mediaFlipViewItemModelFactory;

        DetailsBarModel = detailsBarModelFactory.Invoke();
        CommandBarModel = flipViewPageCommandBarModelFactory.Invoke(SelectPreviousCommand, SelectNextCommand);

        messenger.Subscribe<MediaItemsLoadedMessage>(msg =>
        {
            Items = new ObservableCollection<IMediaItem>(msg.MediaItems);
            SelectedItem = msg.StartItem;
            ShowNoItemsUI = !Items.Any();
        });

        messenger.Subscribe<MediaItemsDeletedMessage>(msg =>
        {
            msg.MediaItems.ForEach(mediaItem => Items.Remove(mediaItem));
        });
    }

    public IMediaFlipViewItemModel? GetFlipViewItemModel(IMediaItem mediaItem)
    {
        return flipViewItemModels.GetValueOrDefault(mediaItem);
    }

    partial void OnSelectedItemChanged(IMediaItem? value)
    {
        UpdateFlipViewItemModels();
        CanSelectPrevious = SelectedItem != null && Items.IndexOf(SelectedItem) > 0;
        CanSelectNext = SelectedItem != null && Items.IndexOf(SelectedItem) < Items.Count - 1;
        var selectedItemModel = SelectedItem is not null ? flipViewItemModels.GetValueOrDefault(SelectedItem) : null;
        DetailsBarModel.SelectedItemModel = selectedItemModel;
        CommandBarModel.SelectedItemModel = selectedItemModel;
    }

    private void UpdateFlipViewItemModels()
    {
        var rangeBefore = flipViewItemModels.Keys.ToList();

        ICollection<IMediaItem> itemRange;

        if (SelectedItem is not null)
        {
            int selectedIndex = Items.IndexOf(SelectedItem);

            int startIndex = Math.Max(selectedIndex - 2, 0);
            int endIndex = Math.Min(selectedIndex + 2, Items.Count - 1);
            itemRange = Items.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        }
        else
        {
            itemRange = Array.Empty<IMediaItem>();
        }

        foreach (var item in rangeBefore.Except(itemRange))
        {
            Log.Info($"Cleanup FlipViewItemModel for {item.Name}");
            flipViewItemModels.Remove(item);
        }

        foreach (var item in itemRange.Except(rangeBefore))
        {
            Log.Info($"Create FlipViewItemModel for {item.Name}");
            flipViewItemModels.Add(item, mediaFlipViewItemModelFactory.Invoke(item));
        }
    }

    [RelayCommand]
    private async void OpenFolder()
    {
        var folderPickerModel = new FolderPickerModel();
        await dialogService.ShowDialogAsync(folderPickerModel);
        if (folderPickerModel.Folder is StorageFolder folder)
        {
            var result = await loadMediaItemsService.LoadMediaItems(folder);
            messenger.Publish(new MediaItemsLoadedMessage(result.MediaItems, result.StartItem));
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevious))]
    private void SelectPrevious()
    {
        SelectedItem = Items[Items.IndexOf(SelectedItem!) - 1];
    }

    [RelayCommand(CanExecute = nameof(CanSelectNext))]
    private void SelectNext()
    {
        SelectedItem = Items[Items.IndexOf(SelectedItem!) + 1];
    }
}
