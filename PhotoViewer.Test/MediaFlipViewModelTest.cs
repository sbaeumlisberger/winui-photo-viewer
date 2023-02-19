using CommunityToolkit.Mvvm.Messaging;
using Moq;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System.Diagnostics;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test;

public class MediaFlipViewModelTest
{
    [Fact]
    public void Receive_MediaFilesLoadedMessage()
    {
        var messenger = new StrongReferenceMessenger();

        Func<IMediaFileInfo, IMediaFlipViewItemModel> itemModelFactory = (mediaFile) => 
        {
            var mock = new Mock<IMediaFlipViewItemModel>();
            mock.SetupGet(m => m.MediaItem).Returns(mediaFile);
            return mock.Object;
        };

        var flipViewPageModel = new MediaFlipViewModel(messenger, null, null, itemModelFactory, new ApplicationSettings());

        List<IMediaFileInfo> files = Enumerable.Range(0, 200).Select(i =>
        {
            var mock = new Mock<IMediaFileInfo>();
            mock.SetupGet(m => m.DisplayName).Returns("File" + i);
            mock.SetupGet(m => m.FilePath).Returns("Test/File" + i);
            return mock.Object;
        }).ToList();
        IMediaFileInfo startFile = files[17];

        messenger.Send(new MediaFilesLoadingMessage(new LoadMediaFilesTask(startFile, Task.FromResult(new LoadMediaFilesResult(files, startFile)))));

        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(startFile, flipViewPageModel.SelectedItemModel.MediaItem);
    }
}