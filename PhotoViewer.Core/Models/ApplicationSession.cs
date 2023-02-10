using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.Core.Utils;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Models;

namespace PhotoViewer.Core.Models;

public class ApplicationSession
{
    public IReadOnlyList<IMediaFileInfo> Files => files;

    private List<IMediaFileInfo> files = new List<IMediaFileInfo>();

    public ApplicationSession(IMessenger messenger)
    {
        messenger.Register<MediaFilesLoadedMessage>(this, msg =>
        {
            files = new List<IMediaFileInfo>(msg.Files);
        });

        messenger.Register<MediaFilesDeletedMessage>(this, msg =>
        {
            msg.Files.ForEach(file => files.Remove(file));
        });
    }

}
