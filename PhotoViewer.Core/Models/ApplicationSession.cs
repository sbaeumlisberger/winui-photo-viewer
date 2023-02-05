using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.Core.Utils;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Models;

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
