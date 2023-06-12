﻿using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.Core.Utils;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Models;

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
            catch 
            { 
                // TODO ?
            }
        });

        messenger.Register<MediaFilesDeletedMessage>(this, msg =>
        {
            msg.Files.ForEach(file => files.Remove(file));
        });
    }

}
