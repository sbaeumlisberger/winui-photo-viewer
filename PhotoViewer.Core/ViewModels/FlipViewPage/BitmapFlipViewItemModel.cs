using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;

namespace PhotoViewer.App.ViewModels;

public interface IBitmapFlipViewItemModel : IMediaFlipViewItemModel
{
    IImageViewModel ImageViewModel { get; }
}

public partial class BitmapFlipViewItemModel : ViewModelBase, IBitmapFlipViewItemModel
{
    public IMediaFileInfo MediaFile { get; }

    public bool IsSelected { get; set; } = false;

    public bool IsDiashowActive { get; set; }

    public IImageViewModel ImageViewModel { get; }

    public bool IsContextMenuEnabeld => IsSelected && !IsDiashowActive;

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public ITagPeopleToolModel? PeopleTagToolModel { get; }

    public ICropImageToolModel CropImageToolModel { get; }

    public bool CanTagPeople => PeopleTagToolModel != null;

    public BitmapFlipViewItemModel(
        IBitmapFileInfo bitmapFile,
        IViewModelFactory viewModelFactory,
        IMessenger messenger) : base(messenger)
    {
        MediaFile = bitmapFile;
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel();
        ContextMenuModel.Files = new[] { bitmapFile };

        ImageViewModel = viewModelFactory.CreateImageViewModel(bitmapFile);
        ImageViewModel.PropertyChanged += ImageViewModel_PropertyChanged;

        if (bitmapFile.IsMetadataSupported)
        {
            PeopleTagToolModel = viewModelFactory.CreateTagPeopleToolModel(bitmapFile);
        }

        CropImageToolModel = viewModelFactory.CreateCropImageToolModel(bitmapFile);
    }

    partial void OnIsContextMenuEnabeldChanged()
    {
        ContextMenuModel.IsEnabled = IsContextMenuEnabeld;
    }

    private void ImageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageViewModel.Image))
        {
            if (PeopleTagToolModel != null)
            {
                PeopleTagToolModel.BitmapImage = ImageViewModel.Image;
            }
        }
    }

    partial void OnIsSelectedChanged()
    {
        if (PeopleTagToolModel != null)
        {
            PeopleTagToolModel.IsEnabled = IsSelected;
        }
        CropImageToolModel.IsEnabled = IsSelected;
    }

    public Task InitializeAsync()
    {
        var loadImageTask = ImageViewModel.InitializeAsync();

        if (PeopleTagToolModel != null)
        {
            return Task.WhenAll(loadImageTask, PeopleTagToolModel.InitializeAsync());
        }

        return loadImageTask;
    }

    protected override void OnCleanup()
    {
        ImageViewModel?.Cleanup();
        PeopleTagToolModel?.Cleanup();
        CropImageToolModel.Cleanup();
        ContextMenuModel.Cleanup();
    }

}
