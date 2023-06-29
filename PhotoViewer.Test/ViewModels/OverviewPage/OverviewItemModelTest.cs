using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.ViewModels.OverviewPage;

public class OverviewItemModelTest
{
    private readonly IMediaFileInfo mediaFile = Substitute.For<IMediaFileInfo>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly OverviewItemModel overviewItemModel;

    public OverviewItemModelTest()
    {
        mediaFile.DisplayName.Returns("TestFile.jpg");
        mediaFile.FileName.Returns("TestFile.jpg");
        mediaFile.FileNameWithoutExtension.Returns("TestFile");

        overviewItemModel = new OverviewItemModel(mediaFile, messenger, metadataService, dialogService);
    }

    [Fact]
    public void ResetsNewNameWhenCancelRenaming() 
    {
        messenger.Send(new ActivateRenameFileMessage(mediaFile));
        overviewItemModel.NewName = "NewName";

        overviewItemModel.CancelRenaming();

        Assert.False(overviewItemModel.IsRenaming);
        Assert.Equal("TestFile", overviewItemModel.NewName);
        Assert.Equal("TestFile.jpg", overviewItemModel.DisplayName);
    }

    [Fact]
    public async Task RenamesMediaFileWhenConfirmRenaming()
    {
        messenger.Send(new ActivateRenameFileMessage(mediaFile));
        await overviewItemModel.LastDispatchTask;
        overviewItemModel.NewName = "NewName";
        mediaFile.RenameAsync("NewName").Returns(Task.CompletedTask)
            .AndDoes(_ => mediaFile.DisplayName.Returns("NewName.jpg"));

        await overviewItemModel.ConfirmRenaming();
       
        await mediaFile.Received().RenameAsync("NewName");
        Assert.False(overviewItemModel.IsRenaming);
        Assert.Equal("NewName.jpg", overviewItemModel.DisplayName);
    }

    [Fact]
    public async Task DoesNotRenameMediaFileWhenNewNameIsEqual()
    {
        messenger.Send(new ActivateRenameFileMessage(mediaFile));
        await overviewItemModel.LastDispatchTask;
        overviewItemModel.NewName = "TestFile";

        await overviewItemModel.ConfirmRenaming();

        await mediaFile.DidNotReceive().RenameAsync(Arg.Any<string>());
        Assert.False(overviewItemModel.IsRenaming);
        Assert.Equal("TestFile", overviewItemModel.NewName);    
        Assert.Equal("TestFile.jpg", overviewItemModel.DisplayName);
    }

    [Fact]
    public async Task DoesNotRenameMediaFileWhenNewNameEmpty()
    {
        messenger.Send(new ActivateRenameFileMessage(mediaFile));
        await overviewItemModel.LastDispatchTask;
        overviewItemModel.NewName = "";

        await overviewItemModel.ConfirmRenaming();

        Assert.True(overviewItemModel.IsRenaming);
        await mediaFile.DidNotReceive().RenameAsync(Arg.Any<string>());
        Assert.Equal("", overviewItemModel.NewName);
        Assert.Equal("TestFile.jpg", overviewItemModel.DisplayName);
    }

    [Fact]
    public async Task ShowErrorDialogWhenRenamingFails()
    {
        messenger.Send(new ActivateRenameFileMessage(mediaFile));
        overviewItemModel.NewName = "NewName";
        mediaFile.RenameAsync("NewName").ThrowsAsync(new Exception("Some Error Message"));

        await overviewItemModel.ConfirmRenaming();
       
        await dialogService.Received().ShowDialogAsync(Arg.Any<MessageDialogModel>());
        Assert.Equal("TestFile", overviewItemModel.NewName);
        Assert.Equal("TestFile.jpg", overviewItemModel.DisplayName);
    }

}
