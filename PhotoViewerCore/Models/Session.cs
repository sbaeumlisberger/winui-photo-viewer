using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerCore.Utils;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.Models;

public class Session
{
    public IList<IMediaFileInfo> MediaItems { get; private set; } = Array.Empty<IMediaFileInfo>();

    public Session(IMessenger messenger)
    {
        messenger.Register<MediaItemsLoadedMessage>(this, msg =>
        {
            MediaItems = new List<IMediaFileInfo>(msg.MediaItems);
        });

        messenger.Register<MediaItemsDeletedMessage>(this, msg =>
        {
            msg.MediaItems.ForEach(mediaItem => MediaItems.Remove(mediaItem));
        });
    }

}
