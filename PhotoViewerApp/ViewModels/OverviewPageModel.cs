using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PhotoViewerApp.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<IMediaItem> items = new ObservableCollection<IMediaItem>();

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    public OverviewPageModel(
        Session session,
        IMessenger messenger,
        IDialogService dialogService)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;

        Items = new ObservableCollection<IMediaItem>(session.MediaItems);

        messenger.Subscribe<MediaItemsLoadedMessage>(msg =>
        {
            Items = new ObservableCollection<IMediaItem>(msg.MediaItems);
        });

        messenger.Subscribe<MediaItemsDeletedMessage>(msg =>
        {
            msg.MediaItems.ForEach(mediaItem => Items.Remove(mediaItem));
        });
    }

    public void ShowItem(IMediaItem mediaItem) 
    {
        messenger.Publish(new NavigateToPageMessage(typeof(FlipViewPageModel), mediaItem));
    }

}
