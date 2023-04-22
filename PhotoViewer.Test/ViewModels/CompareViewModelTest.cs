using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.ViewModels;

public class CompareViewModelTest
{

    private readonly CompareViewModel compareViewModel;

    private readonly ObservableList<IBitmapFileInfo> bitmapFiles = new ObservableList<IBitmapFileInfo>();

    private readonly IImageLoaderService imageLoaderService = Substitute.For<IImageLoaderService>();

    private readonly IDeleteFilesCommand deleteFilesCommand = Substitute.For<IDeleteFilesCommand>();

    public CompareViewModelTest() 
    {
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 01.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 02.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 03.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 04.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 05.jpg"));

        compareViewModel = new CompareViewModel(bitmapFiles, new ApplicationSettings(), imageLoaderService, deleteFilesCommand);
    }


    [Fact]
    public void SelectsNextFile_WhenSelectedFileRemoved() 
    {
        compareViewModel.SelectedBitmapFile = bitmapFiles[2];

        bitmapFiles.Remove(compareViewModel.SelectedBitmapFile);

        Assert.Equal(bitmapFiles[2], compareViewModel.SelectedBitmapFile);
    }

    [Fact]
    public void SelectsPreviousFile_WhenLastFileSelectedAndRemoved()
    {
        compareViewModel.SelectedBitmapFile = bitmapFiles[4];

        bitmapFiles.Remove(compareViewModel.SelectedBitmapFile);

        Assert.Equal(bitmapFiles[3], compareViewModel.SelectedBitmapFile);
    }

    [Fact]
    public void ClearsSelectedFile_WhenAllFilesRemoved()
    {
        compareViewModel.SelectedBitmapFile = bitmapFiles[2];

        bitmapFiles.Clear();

        Assert.Null(compareViewModel.SelectedBitmapFile);
    }

    [Fact]
    public void ImageIsLoaded_WhenSelectedBitmapFileChanged()
    {
        var bitmapFile = bitmapFiles[2];
        var image = Substitute.For<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile, Arg.Any<CancellationToken>()).Returns(image);
        
        compareViewModel.SelectedBitmapFile = bitmapFile;

        Assert.Equal(image, compareViewModel.BitmapImage);
    }

    [Fact]
    public void PreviousImageIsDisposed_WhenLoadingNewImage()
    {
        var bitmapFile1 = bitmapFiles[0];
        var image1 = Substitute.For<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile1, Arg.Any<CancellationToken>()).Returns(image1);
        compareViewModel.SelectedBitmapFile = bitmapFile1;
        var bitmapFile2 = bitmapFiles[1];
        var image2 = Substitute.For<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile2, Arg.Any<CancellationToken>()).Returns(image2);

        compareViewModel.SelectedBitmapFile = bitmapFile2;

        image1.Received().Dispose();
        Assert.Equal(image2, compareViewModel.BitmapImage);
    }

    [Fact]
    public void LoadingImageIsCanceld_WhenLoadingNewImage()
    {
        var bitmapFile1 = bitmapFiles[0];
        var image1 = Substitute.For<IBitmapImageModel>();
        var image1TSC = new TaskCompletionSource<IBitmapImageModel>();
        CancellationToken? image1CancellationToken = null;
        imageLoaderService.LoadFromFileAsync(bitmapFile1, Arg.Do<CancellationToken>(value => image1CancellationToken = value)).Returns(image1TSC.Task);
        compareViewModel.SelectedBitmapFile = bitmapFile1;
        var bitmapFile2 = bitmapFiles[1];
        var image2 = Substitute.For<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile2, Arg.Any<CancellationToken>()).Returns(image2);

        compareViewModel.SelectedBitmapFile = bitmapFile2;
        image1TSC.SetResult(image1);

        Assert.True(image1CancellationToken?.IsCancellationRequested);
        image1.Received().Dispose();
        Assert.Equal(image2, compareViewModel.BitmapImage);
    }


    [Fact]
    public void ClearsImage_WhenSelectedBitmapFileChangedToNull()
    {
        var bitmapFile = bitmapFiles[2];
        var image = Substitute.For<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile, Arg.Any<CancellationToken>()).Returns(image);
        compareViewModel.SelectedBitmapFile = bitmapFile;
        Assert.NotNull(compareViewModel.BitmapImage);

        compareViewModel.SelectedBitmapFile = null;

        Assert.Null(compareViewModel.BitmapImage);
    }

    [Fact]
    public void ClearsImage_BeforeLoadingNewImage()
    {
        var bitmapFile1 = bitmapFiles[0];
        var image1 = Substitute.For<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile1, Arg.Any<CancellationToken>()).Returns(image1);
        compareViewModel.SelectedBitmapFile = bitmapFile1;
        var bitmapFile2 = bitmapFiles[1];
        var image2 = Substitute.For<IBitmapImageModel>();
        var image2TSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoaderService.LoadFromFileAsync(bitmapFile2, Arg.Any<CancellationToken>()).Returns(image2TSC.Task);

        compareViewModel.SelectedBitmapFile = bitmapFile2;

        Assert.Null(compareViewModel.BitmapImage);

        image2TSC.SetResult(image2);

        Assert.Equal(image2, compareViewModel.BitmapImage);
    }


    [Fact]
    public void HandelsImageLoadingErrors()
    {
        var bitmapFile = bitmapFiles[2];
        imageLoaderService.LoadFromFileAsync(bitmapFile, Arg.Any<CancellationToken>()).Throws(new Exception());
        compareViewModel.SelectedBitmapFile = bitmapFile;

        Assert.Null(compareViewModel.BitmapImage);
    }
}
