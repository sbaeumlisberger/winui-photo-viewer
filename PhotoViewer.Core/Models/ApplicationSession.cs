using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.Models;

public interface IApplicationSession
{
    IReadOnlyList<IMediaFileInfo> Files { get; }
}

public class ApplicationSession : IApplicationSession
{
    public IReadOnlyList<IMediaFileInfo> Files => files;

    private List<IMediaFileInfo> files = new List<IMediaFileInfo>();

    public ApplicationSession(IMessenger messenger)
    {
        messenger.Register<MediaFilesLoadingMessage>(this, async msg =>
        {
            try
            {
                files = new List<IMediaFileInfo>((await msg.LoadMediaFilesTask.WaitForResultAsync()).MediaFiles);
            }
            catch { /* errors are handled at page level */ }
        });

        messenger.Register<MediaFilesDeletedMessage>(this, msg =>
        {
            msg.Files.ForEach(file => files.Remove(file));
        });
    }

}
