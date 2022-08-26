using PhotoViewerApp.Messages;
using PhotoViewerApp.Utils;
using System;
using System.Collections.Generic;

namespace PhotoViewerApp.Models;

public class Session
{
    public static Session Instance { get; } = new Session();

    public IList<IMediaItem> MediaItems { get; private set; } = Array.Empty<IMediaItem>();

    public Session()
    {
        Messenger.GlobalInstance.Subscribe<MediaItemsLoadedMessage>(msg =>
        {
            MediaItems = new List<IMediaItem>(msg.MediaItems);
        });

        Messenger.GlobalInstance.Subscribe<MediaItemsDeletedMessage>(msg =>
        { 
            msg.MediaItems.ForEach(mediaItem => MediaItems.Remove(mediaItem)); 
        });
    }

}
