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
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    public IMetadataPanelModel MetadataPanelModel { get; }

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public ObservableCollection<IMediaFileInfo> Items { get; private set; } = new ObservableCollection<IMediaFileInfo>();

    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IOverviewPageCommandBarModel OverviewPageCommandBarModel { get; }

    public OverviewPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IOverviewPageCommandBarModel overviewPageCommandBarModel,
        IMediaFileContextMenuModel mediaFileContextMenuModel,
        MetadataPanelModelFactory metadataPanelModelFactory) : base(messenger)
    {
        OverviewPageCommandBarModel = overviewPageCommandBarModel;
        MetadataPanelModel = metadataPanelModelFactory.Invoke(false);
        ContextMenuModel = mediaFileContextMenuModel;

        Messenger.Register<MediaFilesLoadedMessage>(this, OnMediaItemsLoadedMessageReceived);
        Messenger.Register<MediaFilesDeletedMessage>(this, OnMediaItemsDeletedMessageReceived);

        Items = new ObservableCollection<IMediaFileInfo>(session.Files);
    }

    public void ShowItem(IMediaFileInfo mediaItem)
    {
        Messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel), mediaItem));
    }

    public OverviewItemModel GetItemModel(IMediaFileInfo mediaFile) 
    {
        return new OverviewItemModel(mediaFile, Messenger, new MetadataService()); // TODO
    }

    private void OnMediaItemsLoadedMessageReceived(MediaFilesLoadedMessage msg)
    {
        Items = new ObservableCollection<IMediaFileInfo>(msg.Files);
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
