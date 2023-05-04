using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Tocronx.SimpleAsync;

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

    private readonly IDeleteFilesCommand deleteFilesCommand;

    private readonly IViewModelFactory viewModelFactory;

    private int selectedIndex = -1;

    private readonly VirualizedCollection<IBitmapFileInfo, IImageViewModel> imageViewModels;

    public CompareViewModel(
        IObservableReadOnlyList<IBitmapFileInfo> bitmapFiles, 
        ApplicationSettings settings, 
        IDeleteFilesCommand deleteFilesCommand, 
        IViewModelFactory viewModelFactory)
    {
        BitmapFiles = bitmapFiles;
        ShowDeleteAnimation = settings.ShowDeleteAnimation;
        this.deleteFilesCommand = deleteFilesCommand;
        this.viewModelFactory = viewModelFactory;

        imageViewModels = new(bitmapFiles, CacheSize, CreateImageViewModel, viewModel => viewModel.Cleanup());

        BitmapFiles.CollectionChanged += BitmapFiles_CollectionChanged;
    }

    protected override void OnCleanup()
    {
        BitmapFiles.CollectionChanged -= BitmapFiles_CollectionChanged;
        imageViewModels.Clear();
    }

    private IImageViewModel CreateImageViewModel(IBitmapFileInfo bitmapFile) 
    {
        var imageViewModel = viewModelFactory.CreateImageViewModel(bitmapFile);
        _ = imageViewModel.InitializeAsync();
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
        ImageViewModel = imageViewModels.SetSelectedItem(SelectedBitmapFile); ;
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
        await deleteFilesCommand.ExecuteAsync(new List<IBitmapFileInfo>() { SelectedBitmapFile! });
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
