using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System.Collections.ObjectModel;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.Models;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core;

namespace PhotoViewer.App.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    public IMetadataPanelModel MetadataPanelModel { get; }

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public ObservableCollection<IMediaFileInfo> Items { get; private set; } = new ObservableCollection<IMediaFileInfo>();

    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IOverviewPageCommandBarModel OverviewPageCommandBarModel { get; }

    private readonly IViewModelFactory viewModelFactory;

    public OverviewPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IViewModelFactory viewModelFactory) : base(messenger)
    {
        this.viewModelFactory = viewModelFactory;

        OverviewPageCommandBarModel = viewModelFactory.CreateOverviewPageCommandBarModel();
        MetadataPanelModel = viewModelFactory.CreateMetadataPanelModel(false);
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel();

        Messenger.Register<MediaFilesLoadingMessage>(this, OnMediaItemsLoadedMessageReceived);
        Messenger.Register<MediaFilesDeletedMessage>(this, OnMediaItemsDeletedMessageReceived);

        Items = new ObservableCollection<IMediaFileInfo>(session.Files);
    }

    protected override void OnCleanup()
    {
        OverviewPageCommandBarModel.Cleanup();
        MetadataPanelModel.Cleanup();
        ContextMenuModel.Cleanup();
    }

    public void ShowItem(IMediaFileInfo mediaItem)
    {
        Messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel), mediaItem));
    }

    public IOverviewItemModel GetItemModel(IMediaFileInfo mediaFile) 
    {
        return viewModelFactory.CreateOverviewItemModel(mediaFile);
    }

    private async void OnMediaItemsLoadedMessageReceived(MediaFilesLoadingMessage msg)
    {
        Items = new ObservableCollection<IMediaFileInfo>((await msg.LoadMediaFilesTask.WaitForResultAsync()).MediaFiles); // TODO error handling
    }

    private void OnMediaItemsDeletedMessageReceived(MediaFilesDeletedMessage msg)
    {
        msg.Files.ForEach(mediaItem => Items.Remove(mediaItem));
    }

    partial void OnSelectedItemsChanged()
    {
        OverviewPageCommandBarModel.SelectedItems = SelectedItems;
        MetadataPanelModel.Files = SelectedItems;
        ContextMenuModel.Files = SelectedItems;
    }

}
