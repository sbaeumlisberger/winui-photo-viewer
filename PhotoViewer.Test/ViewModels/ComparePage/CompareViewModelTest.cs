using Essentials.NET;
using NSubstitute;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using Xunit;

namespace PhotoViewer.Test.ViewModels;

public class CompareViewModelTest
{

    private readonly CompareViewModel compareViewModel;

    private readonly ObservableList<IBitmapFileInfo> bitmapFiles = new ObservableList<IBitmapFileInfo>();

    private readonly IDeleteFilesService deleteFilesCommand = Substitute.For<IDeleteFilesService>();

    private readonly IViewModelFactory viewModelFactory = Substitute.For<IViewModelFactory>();

    public CompareViewModelTest()
    {
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 01.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 02.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 03.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 04.jpg"));
        bitmapFiles.Add(TestUtils.MockBitmapFile("File 05.jpg"));

        viewModelFactory.CreateImageViewModel(Arg.Any<IBitmapFileInfo>()).ReturnsForAnyArgs(_ => Substitute.For<IImageViewModel>());

        compareViewModel = new CompareViewModel(bitmapFiles, new ApplicationSettings(), deleteFilesCommand, viewModelFactory);
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
        var imageViewModel = Substitute.For<IImageViewModel>();
        viewModelFactory.CreateImageViewModel(bitmapFile).Returns(_ => imageViewModel);

        compareViewModel.SelectedBitmapFile = bitmapFile;

        Assert.Equal(imageViewModel, compareViewModel.ImageViewModel);
    }

}
