using CommunityToolkit.Mvvm.Input;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Specialized;

namespace PhotoViewer.Core.ViewModels;

public record struct ViewState(float ZoomFactor, double HorizontalOffset, double VerticalOffset);

public interface ICompareViewModel : IViewModel
{
    event EventHandler<ViewState>? ViewChangedByUser;

    IBitmapFileInfo? SelectedBitmapFile { get; set; }

    void ChangeView(float zoomFactor, double horizontalOffset, double verticalOffset);
}

public partial class CompareViewModel : ViewModelBase, ICompareViewModel
{
    private const int CacheSize = 1;

    public event EventHandler<ViewState>? ViewChangedByUser;

    public event EventHandler<ViewState>? ViewChangeRequested;

    public IObservableReadOnlyList<IBitmapFileInfo> BitmapFiles { get; }

    public IBitmapFileInfo? SelectedBitmapFile { get; set; }

    public IImageViewModel? ImageViewModel { get; private set; }

    public bool CanSelectPrevious => SelectedBitmapFile != null && SelectedBitmapFile != BitmapFiles.First();

    public bool CanSelectNext => SelectedBitmapFile != null && SelectedBitmapFile != BitmapFiles.Last();

    public bool CanDelete => SelectedBitmapFile != null;

    public bool ShowDeleteAnimation { get; }

    private readonly IDeleteFilesService deleteFilesService;

    private readonly IViewModelFactory viewModelFactory;

    private int selectedIndex = -1;

    private readonly VirtualizedCollection<IBitmapFileInfo, IImageViewModel> imageViewModelsCache;

    public CompareViewModel(
        IObservableReadOnlyList<IBitmapFileInfo> bitmapFiles,
        ApplicationSettings settings,
        IDeleteFilesService deleteFilesService,
        IViewModelFactory viewModelFactory)
    {
        BitmapFiles = bitmapFiles;
        ShowDeleteAnimation = settings.ShowDeleteAnimation;
        this.deleteFilesService = deleteFilesService;
        this.viewModelFactory = viewModelFactory;

        imageViewModelsCache = VirtualizedCollection.Create(CacheSize, CreateImageViewModel, viewModel => viewModel.Cleanup(), bitmapFiles);

        BitmapFiles.CollectionChanged += BitmapFiles_CollectionChanged;
    }

    protected override void OnCleanup()
    {
        BitmapFiles.CollectionChanged -= BitmapFiles_CollectionChanged;
        imageViewModelsCache.ClearCache();
    }

    private IImageViewModel CreateImageViewModel(IBitmapFileInfo bitmapFile)
    {
        var imageViewModel = viewModelFactory.CreateImageViewModel(bitmapFile);
        imageViewModel.InitializeAsync().LogOnException();
        return imageViewModel;
    }

    private void BitmapFiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedBitmapFile != null && !BitmapFiles.Contains(SelectedBitmapFile))
        {
            if (selectedIndex < BitmapFiles.Count)
            {
                SelectedBitmapFile = BitmapFiles[selectedIndex];
            }
            else
            {
                SelectedBitmapFile = BitmapFiles.LastOrDefault();
            }
        }
    }

    partial void OnSelectedBitmapFileChanged()
    {
        selectedIndex = SelectedBitmapFile != null ? BitmapFiles.IndexOf(SelectedBitmapFile) : -1;
        ImageViewModel = imageViewModelsCache.SetSelectedItem(SelectedBitmapFile);
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevious))]
    private void SelectPrevious()
    {
        SelectedBitmapFile = BitmapFiles[BitmapFiles.IndexOf(SelectedBitmapFile!) - 1];
    }

    [RelayCommand(CanExecute = nameof(CanSelectNext))]
    private void SelectNext()
    {
        SelectedBitmapFile = BitmapFiles[BitmapFiles.IndexOf(SelectedBitmapFile!) + 1];
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        Log.Debug($"Delete {SelectedBitmapFile!.DisplayName} via command bar");
        await deleteFilesService.DeleteFilesAsync([SelectedBitmapFile!]);
    }

    public void OnViewChangedByUser(float zoomFactor, double horizontalOffset, double verticalOffset)
    {
        ViewChangedByUser?.Invoke(this, new ViewState(zoomFactor, horizontalOffset, verticalOffset));
    }

    public void ChangeView(float zoomFactor, double horizontalOffset, double verticalOffset)
    {
        ViewChangeRequested?.Invoke(this, new ViewState(zoomFactor, horizontalOffset, verticalOffset));
    }
}
