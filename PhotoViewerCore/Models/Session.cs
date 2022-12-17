using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerCore.Utils;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.Models;

public class Session
{
    public IList<IMediaFileInfo> Files { get; private set; } = Array.Empty<IMediaFileInfo>();

    public Session(IMessenger messenger)
    {
        messenger.Register<MediaFilesLoadedMessage>(this, msg =>
        {
            Files = new List<IMediaFileInfo>(msg.Files);
        });

        messenger.Register<MediaFilesDeletedMessage>(this, msg =>
        {
            msg.Files.ForEach(file => Files.Remove(file));
        });
    }

}
