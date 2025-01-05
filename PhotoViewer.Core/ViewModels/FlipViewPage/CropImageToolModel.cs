using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public interface ICropImageToolModel : IViewModel
{
    float UIScaleFactor { get; set; }

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
    public partial float UIScaleFactor { get; set; } = 1;

    public partial bool IsEnabled { get; set; }

    public partial bool IsActive { get; private set; }

    public partial SizeInt32 ImageSizeInPixels { get; private set; } = default;

    public RectInt32 SelectionInPixels => new RectInt32((int)SelectionXInPixels, (int)SelectionYInPixels, (int)SelectionWidthInPixels, (int)SelectionHeightInPixels);

    public partial double SelectionXInPixels { get; set; }

    public partial double SelectionYInPixels { get; set; }

    public partial double SelectionWidthInPixels { get; set; }

    public partial double SelectionHeightInPixels { get; set; }

    public ReadOnlyCollection<AspectRadioMode> AvailableAspectRadioModes = Enum.GetValues<AspectRadioMode>().AsReadOnly();

    public partial AspectRadioMode AspectRadioMode { get; set; } = AspectRadioMode.Orginal;

    public bool IsFixedAspectRadio => AspectRadioMode == AspectRadioMode.Fixed;

    public partial double AspectRadioWidth { get; set; } = 3;

    public partial double AspectRadioHeight { get; set; } = 2;

    public Size AspectRadio => AspectRadioMode == AspectRadioMode.Fixed
        ? new Size(AspectRadioWidth, AspectRadioHeight)
        : AspectRadioMode == AspectRadioMode.Orginal
            ? new Size(ImageSizeInPixels.Width, ImageSizeInPixels.Height)
            : Size.Empty;

    public bool CanSave => SelectionWidthInPixels >= 1 && SelectionHeightInPixels >= 1;

    private readonly ICropImageService cropImageService;

    private readonly IDialogService dialogService;

    private readonly IBitmapFileInfo bitmapFile;

    internal CropImageToolModel(IBitmapFileInfo bitmapFile, IMessenger messenger, ICropImageService cropImageService, IDialogService dialogService) : base(messenger)
    {
        this.bitmapFile = bitmapFile;
        this.cropImageService = cropImageService;
        this.dialogService = dialogService;

        Register<ToggleCropImageToolMessage>(Receive);
        Register<BitmapModifiedMesssage>(Receive);

        LoadImageSizeAsync().LogOnException();
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

    partial void OnIsActiveChanged()
    {
        if (!IsActive)
        {
            ResetSelection();
        }
    }

    partial void OnSelectionWidthInPixelsChanged()
    {
        if (AspectRadio != Size.Empty)
        {
            double aspectRadio = AspectRadio.Width / AspectRadio.Height;
            SelectionHeightInPixels = (int)Math.Round(SelectionWidthInPixels / aspectRadio);
        }
    }

    partial void OnSelectionHeightInPixelsChanged()
    {
        if (AspectRadio != Size.Empty)
        {
            double aspectRadio = AspectRadio.Width / AspectRadio.Height;
            SelectionWidthInPixels = (int)Math.Round(SelectionHeightInPixels * aspectRadio);
        }
    }    

    private async Task LoadImageSizeAsync()
    {
        var imageSizeInPixels = await bitmapFile.GetSizeInPixelsAsync();
        ImageSizeInPixels = new SizeInt32((int)imageSizeInPixels.Width, (int)imageSizeInPixels.Height);
        ResetSelection();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
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

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveCopyAsync()
    {
        var filePickerModel = new FileSavePickerModel()
        {
            SuggestedFileName = bitmapFile.FileName,
            FileTypeChoices = new()
            {
                {
                    bitmapFile.FileExtension.TrimStart('.').ToUpper(),
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

    private void ResetSelection()
    {
        SelectionXInPixels = Math.Round(ImageSizeInPixels.Width * 0.1d);
        SelectionYInPixels = Math.Round(ImageSizeInPixels.Height * 0.1d);
        SelectionWidthInPixels = Math.Round(ImageSizeInPixels.Width * 0.8d);
        SelectionHeightInPixels = Math.Round(ImageSizeInPixels.Height * 0.8d);
    }
}
