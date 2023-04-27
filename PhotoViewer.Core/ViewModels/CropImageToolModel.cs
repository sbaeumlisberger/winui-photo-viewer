using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PropertyChanged;
using Tocronx.SimpleAsync;
using Windows.Foundation;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public interface ICropImageToolModel : IViewModel
{
    bool IsEnabled { get; set; }
}

public enum AspectRadioMode
{
    Orginal,
    Free,
    Fixed
}

public partial class CropImageToolModel : ViewModelBase, ICropImageToolModel
{
    public bool IsEnabled { get; set; }

    public bool IsActive { get; private set; }

    [DoNotCheckEquality] // TODO
    public Size ImageSizeInPixels { get; private set; } = Size.Empty;

    public Rect SelectionInPixels { get; set; } = Rect.Empty;

    public IReadOnlyList<AspectRadioMode> AvailableAspectRadioModes = Enum.GetValues<AspectRadioMode>();

    public AspectRadioMode AspectRadioMode { get; set; } = AspectRadioMode.Orginal;

    public bool IsFixedAspectRadio => AspectRadioMode == AspectRadioMode.Fixed;

    public double AspectRadioWidth { get; set; } = 3;

    public double AspectRadioHeight { get; set; } = 2;

    public Size AspectRadio => AspectRadioMode == AspectRadioMode.Fixed
        ? new Size(AspectRadioWidth, AspectRadioHeight)
        : AspectRadioMode == AspectRadioMode.Orginal && !ImageSizeInPixels.IsEmpty
            ? ImageSizeInPixels
            : Size.Empty;

    private readonly ICropImageService cropImageService;

    private readonly IDialogService dialogService;

    private readonly IBitmapFileInfo bitmapFile;

    public CropImageToolModel(IBitmapFileInfo bitmapFile, IMessenger messenger, ICropImageService cropImageService, IDialogService dialogService) : base(messenger)
    {
        this.bitmapFile = bitmapFile;
        this.cropImageService = cropImageService;
        this.dialogService = dialogService;

        Register<ToggleCropImageToolMessage>(Receive);
        Register<BitmapModifiedMesssage>(Receive);

        LoadImageSizeAsync().FireAndForget();
    }

    private void Receive(ToggleCropImageToolMessage msg)
    {
        if (IsEnabled)
        {
            IsActive = !IsActive;
        }
    }

    private async void Receive(BitmapModifiedMesssage msg)
    {
        if (msg.BitmapFile.Equals(bitmapFile))
        {
            await LoadImageSizeAsync();
        }
    }

    partial void OnIsEnabledChanged()
    {
        if (!IsEnabled)
        {
            IsActive = false;
        }
    }

    private async Task LoadImageSizeAsync()
    {
        ImageSizeInPixels = await bitmapFile.GetSizeInPixelsAsync();

        SelectionInPixels = new Rect(
            ImageSizeInPixels.Width * 0.1,
            ImageSizeInPixels.Height * 0.1,
            ImageSizeInPixels.Width * 0.8,
            ImageSizeInPixels.Height * 0.8);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await cropImageService.CropImageAsync(bitmapFile, SelectionInPixels);

            IsActive = false;
        }
        catch (Exception ex)
        {
            await HandleExceptionOnSaveAsync(ex);
        }
    }

    [RelayCommand]
    private async Task SaveCopyAsync()
    {
        var filePickerModel = new FileSavePickerModel()
        {
            SuggestedFileName = bitmapFile.FileName,
            FileTypeChoices = new()
            {
                {
                    bitmapFile.FileExtension.StripStart(".").ToUpper(),
                    new[] { bitmapFile.FileExtension }
                }
            }
        };
        await dialogService.ShowDialogAsync(filePickerModel);

        if (filePickerModel.File is { } file)
        {
            try
            {
                using var fileStream = (await file.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
                await cropImageService.CropImageAsync(bitmapFile, SelectionInPixels, fileStream);
                IsActive = false;
            }
            catch (Exception ex)
            {
                await HandleExceptionOnSaveAsync(ex);
            }
        }
    }

    private async Task HandleExceptionOnSaveAsync(Exception ex) 
    {
        Log.Error($"Failed to crop image {bitmapFile.FileName}", ex);

        await dialogService.ShowDialogAsync(new MessageDialogModel()
        {
            Title = Strings.CropImageErrorDialogTitle,
            Message = ex.Message
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        IsActive = false;
    }
}
