using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.ViewModels;
using System.Collections.ObjectModel;

namespace PhotoViewerApp.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    public IMetadataPanelModel MetadataPanelModel { get; }

    public ObservableCollection<IMediaFileInfo> Items { get; private set; } = new ObservableCollection<IMediaFileInfo>();

    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IOverviewPageCommandBarModel OverviewPageCommandBarModel { get; }

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    public OverviewPageModel(
        Session session,
        IMessenger messenger,
        IDialogService dialogService,
        IOverviewPageCommandBarModel overviewPageCommandBarModel,
        MetadataPanelModelFactory metadataPanelModelFactory)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;

        OverviewPageCommandBarModel = overviewPageCommandBarModel;
        MetadataPanelModel = metadataPanelModelFactory.Invoke(false);

        Items = new ObservableCollection<IMediaFileInfo>(session.MediaItems);

        messenger.Subscribe<MediaItemsLoadedMessage>(msg =>
        {
            Items = new ObservableCollection<IMediaFileInfo>(msg.MediaItems);
        });

        messenger.Subscribe<MediaItemsDeletedMessage>(msg =>
        {
            msg.MediaItems.ForEach(mediaItem => Items.Remove(mediaItem));
        });
    }

    public void ShowItem(IMediaFileInfo mediaItem)
    {
        messenger.Publish(new NavigateToPageMessage(typeof(FlipViewPageModel), mediaItem));
    }

    partial void OnSelectedItemsChanged()
    {
        OverviewPageCommandBarModel.SelectedItems = SelectedItems;
        MetadataPanelModel.Files = SelectedItems;
    }

}
