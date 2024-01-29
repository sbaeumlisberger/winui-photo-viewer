using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using NSubstitute;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.ViewModels;
using Xunit;

namespace PhotoViewer.Test.ViewModels;

public class ComparePageModelTest
{

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IReadOnlyList<IBitmapFileInfo> bitmapFiles;

    private readonly ICompareViewModel left = Substitute.For<ICompareViewModel>();

    private readonly ICompareViewModel right = Substitute.For<ICompareViewModel>();

    private readonly ComparePageModel comparePageModel;

    public ComparePageModelTest()
    {
        bitmapFiles = new[]
        {
            TestUtils.MockBitmapFile("File 01.jpg"),
            TestUtils.MockBitmapFile("File 02.jpg"),
            TestUtils.MockBitmapFile("File 03.jpg"),
            TestUtils.MockBitmapFile("File 04.jpg"),
            TestUtils.MockBitmapFile("File 05.jpg"),
        };

        var session = Substitute.For<IApplicationSession>();
        session.Files.Returns(bitmapFiles);

        var viewModelFactory = Substitute.For<IViewModelFactory>();
        viewModelFactory.CreateCompareViewModel(Arg.Any<IObservableList<IBitmapFileInfo>>()).Returns(left, right);

        comparePageModel = new ComparePageModel(session, messenger, viewModelFactory);
    }

    [Fact]
    public void ChangesWindowTitle_WhenNavigatedTo()
    {
        var messageCapture = TestUtils.CaptureMessage<ChangeWindowTitleMessage>(messenger);

        comparePageModel.OnNavigatedTo(bitmapFiles[0]);

        Assert.Equal(Strings.ComparePage_Title, messageCapture.Message.NewTitle);
    }

    [Fact]
    public void InitializesLeftAndRightView_WhenNavigatedTo()
    {
        comparePageModel.OnNavigatedTo(bitmapFiles[0]);

        Assert.Equal(left.SelectedBitmapFile, bitmapFiles[0]);
        Assert.Equal(right.SelectedBitmapFile, bitmapFiles[1]);
    }

    [Fact]
    public void InitializesLeftViewOnly_WhenNavigatedTo_LastFileAsParameter()
    {
        comparePageModel.OnNavigatedTo(bitmapFiles.Last());

        Assert.Equal(left.SelectedBitmapFile, bitmapFiles.Last());
        Assert.Equal(right.SelectedBitmapFile, bitmapFiles.Last());
    }

}
