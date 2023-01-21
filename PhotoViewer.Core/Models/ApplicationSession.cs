using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerCore.Utils;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Models;

namespace PhotoViewer.Core.Models;

public class ApplicationSession
{
    public IList<IMediaFileInfo> Files { get; private set; } = Array.Empty<IMediaFileInfo>();

    public ApplicationSession(IMessenger messenger)
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
