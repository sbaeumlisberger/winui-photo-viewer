using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.Models;

public interface IApplicationSession
{
    IReadOnlyList<IMediaFileInfo> Files { get; }
}

internal class ApplicationSession : IApplicationSession
{
    public IReadOnlyList<IMediaFileInfo> Files => files;

    public SortBy SortBy { get; private set; } = SortBy.Unspecified;

    public bool IsSortedDescending { get; private set; } = false;

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
        
        messenger.Register<MediaFileRestoredMessage>(this, msg =>
        {
            files.Insert(msg.Index, msg.File);
        });       

        messenger.Register<FilesSortedMessage>(this, msg =>
        {
            files = new List<IMediaFileInfo>(msg.SortedFiles);
            SortBy = msg.SortBy;
            IsSortedDescending = msg.Descending;
        });
    }

}
