using PhotoViewerApp.Messages;
using PhotoViewerApp.Utils;
using System;
using System.Collections.Generic;

namespace PhotoViewerApp.Models;

public class Session
{
    public static Session Instance { get; } = new Session();

    public IList<IMediaFileInfo> MediaItems { get; private set; } = Array.Empty<IMediaFileInfo>();

    public Session()
    {
        Messenger.GlobalInstance.Subscribe<MediaItemsLoadedMessage>(msg =>
        {
            MediaItems = new List<IMediaFileInfo>(msg.MediaItems);
        });

        Messenger.GlobalInstance.Subscribe<MediaItemsDeletedMessage>(msg =>
        { 
            msg.MediaItems.ForEach(mediaItem => MediaItems.Remove(mediaItem)); 
        });
    }

}
