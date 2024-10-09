using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public interface IImageViewModel : IViewModel
{
    IBitmapImageModel? Image { get; }

    bool IsLoading { get; }

    bool IsLoadingImageFailed { get; }

    string ErrorMessage { get; }

    IAsyncRelayCommand ReloadCommand { get; }

    Task InitializeAsync();
}

public partial class ImageViewModel : ViewModelBase, IImageViewModel
{
    public IBitmapImageModel? Image { get; private set; }

    public bool IsLoading { get; private set; } = false;

    public bool IsLoadingImageFailed { get; private set; } = false;

    public string ErrorMessage { get; private set; } = string.Empty;

    private readonly ICachedImageLoaderService imageService;

    private readonly CancelableTaskRunner loadImageRunner = new CancelableTaskRunner();

    private readonly IBitmapFileInfo bitmapFile;

    public ImageViewModel(IBitmapFileInfo bitmapFile, ICachedImageLoaderService imageService, IMessenger messenger) : base(messenger)
    {
        this.imageService = imageService;
        this.bitmapFile = bitmapFile;
        Register<BitmapModifiedMesssage>(OnReceive);
    }

    public Task InitializeAsync()
    {
        return LoadAsync();
    }

    protected override void OnCleanup()
    {
        loadImageRunner.Cancel();
        Image.DisposeSafely(() => Image = null);
    }

    private async Task LoadAsync(bool reload = false)
    {
        await loadImageRunner.RunAndCancelPrevious(async (cancellationToken) =>
        {
            try
            {
                Image.DisposeSafely(() => Image = null);

                IsLoading = true;
                IsLoadingImageFailed = false;
                ErrorMessage = string.Empty;

                var image = await imageService.LoadFromFileAsync(bitmapFile, cancellationToken, reload);
                Log.Debug("LoadFromFileAsync completed");
                cancellationToken.ThrowIfCancellationRequested();
                Image = image;

                Log.Debug("send BitmapImageLoadedMessage");
                Messenger.Send(new BitmapImageLoadedMessage(bitmapFile, Image));
            }
            catch (OperationCanceledException)
            {
                Log.Debug($"Canceled loading {bitmapFile.FileName}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load {bitmapFile.FileName}", ex);
                IsLoadingImageFailed = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    private async void OnReceive(BitmapModifiedMesssage msg)
    {
        if (msg.BitmapFile.Equals(bitmapFile))
        {
            await LoadAsync(reload: true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsLoadingImageFailed))]
    private async Task ReloadAsync()
    {
        await LoadAsync(reload: true);
    }
}