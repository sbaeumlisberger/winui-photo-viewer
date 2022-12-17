using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.ViewModels;
using System.Collections.ObjectModel;
using PhotoViewerCore.Utils;

namespace PhotoViewerApp.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    public IMetadataPanelModel MetadataPanelModel { get; }

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public ObservableCollection<IMediaFileInfo> Items { get; private set; } = new ObservableCollection<IMediaFileInfo>();

    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IOverviewPageCommandBarModel OverviewPageCommandBarModel { get; }

    private readonly Session session;

    private readonly IDialogService dialogService;

    public OverviewPageModel(
        Session session,
        IMessenger messenger,
        IDialogService dialogService,
        IOverviewPageCommandBarModel overviewPageCommandBarModel,
        IMediaFileContextMenuModel mediaFileContextMenuModel,
        MetadataPanelModelFactory metadataPanelModelFactory) : base(messenger)
    {
        this.session = session;
        this.dialogService = dialogService;

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
