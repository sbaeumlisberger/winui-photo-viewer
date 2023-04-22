using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using System.Collections.Specialized;
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
    public event EventHandler<ViewState>? ViewChangedByUser;

    public event EventHandler<ViewState>? ViewChangeRequested;

    public IObservableReadOnlyList<IBitmapFileInfo> BitmapFiles { get; }

    public IBitmapFileInfo? SelectedBitmapFile { get; set; }

    public IBitmapImageModel? BitmapImage { get; private set; } // TODO add caching

    public bool CanSelectPrevious => SelectedBitmapFile != null && SelectedBitmapFile != BitmapFiles.First();

    public bool CanSelectNext => SelectedBitmapFile != null && SelectedBitmapFile != BitmapFiles.Last();

    public bool CanDelete => SelectedBitmapFile != null;

    private readonly IImageLoaderService imageLoaderService;

    private readonly IDeleteFilesCommand deleteFilesCommand;

    private readonly CancelableTaskRunner loadImageTaskRunner = new CancelableTaskRunner();

    private int selectedIndex = -1;

    public CompareViewModel(IObservableReadOnlyList<IBitmapFileInfo> bitmapFiles, IImageLoaderService imageLoaderService, IDeleteFilesCommand deleteFilesCommand)
    {
        BitmapFiles = bitmapFiles;
        this.deleteFilesCommand = deleteFilesCommand;
        this.imageLoaderService = imageLoaderService;

        BitmapFiles.CollectionChanged += BitmapFiles_CollectionChanged;
    }

    protected override void OnCleanup()
    {
        BitmapFiles.CollectionChanged -= BitmapFiles_CollectionChanged;
        loadImageTaskRunner.Cancel();
        BitmapImage?.DisposeSafely(() => BitmapImage = null);
    }

    private void BitmapFiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if(SelectedBitmapFile != null && !BitmapFiles.Contains(SelectedBitmapFile)) 
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
        loadImageTaskRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            selectedIndex = SelectedBitmapFile != null ? BitmapFiles.IndexOf(SelectedBitmapFile) : -1;

            BitmapImage?.DisposeSafely(() => BitmapImage = null);

            if (SelectedBitmapFile is { } bitmapFile)
            {
                try
                {
                    var bitmapImage = await imageLoaderService.LoadFromFileAsync(bitmapFile, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        bitmapImage.Dispose();
                    }
                    cancellationToken.ThrowIfCancellationRequested();

                    BitmapImage = bitmapImage;
                }
                catch(Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Error($"Failed to load image {bitmapFile.FileName}", ex);
                }
            }
            else
            {
                BitmapImage = null;
            }
        });   
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
