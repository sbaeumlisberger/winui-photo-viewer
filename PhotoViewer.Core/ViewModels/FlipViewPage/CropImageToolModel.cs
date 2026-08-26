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

public enum AspectRatioMode
{
    Original,
    Free,
    Fixed
}

public partial class CropImageToolModel : ViewModelBase, ICropImageToolModel
{
    private enum AspectRatioAdjustment
    {
        Fit,
        Width,
        Height
    }

    public partial float UIScaleFactor { get; set; } = 1;

    public partial bool IsEnabled { get; set; }

    public partial bool IsActive { get; private set; }

    public partial SizeInt32 ImageSizeInPixels { get; private set; } = default;

    public partial RectInt32 SelectionInPixels { get; private set; }

    public double SelectionWidthInPixels
    {
        get => SelectionInPixels.Width;
        set
        {
            if (!double.IsFinite(value))
            {
                return;
            }

            var previousSelection = SelectionInPixels;
            SetSelectionInPixels(new RectInt32(
                SelectionInPixels.X,
                SelectionInPixels.Y,
                (int)value,
                SelectionInPixels.Height), AspectRatioAdjustment.Width);

            if (SelectionInPixels == previousSelection)
            {
                OnPropertyChanged(nameof(SelectionWidthInPixels));
            }
        }
    }

    public double SelectionHeightInPixels
    {
        get => SelectionInPixels.Height;
        set
        {
            if (!double.IsFinite(value))
            {
                return;
            }

            var previousSelection = SelectionInPixels;
            SetSelectionInPixels(new RectInt32(
                SelectionInPixels.X,
                SelectionInPixels.Y,
                SelectionInPixels.Width,
                (int)value), AspectRatioAdjustment.Height);

            if (SelectionInPixels == previousSelection)
            {
                OnPropertyChanged(nameof(SelectionHeightInPixels));
            }
        }
    }

    public ReadOnlyCollection<AspectRatioMode> AvailableAspectRatioModes = Enum.GetValues<AspectRatioMode>().AsReadOnly();

    public partial AspectRatioMode AspectRatioMode { get; set; } = AspectRatioMode.Original;

    public bool IsFixedAspectRatio => AspectRatioMode == AspectRatioMode.Fixed;

    public partial double AspectRatioWidth { get; set; } = 3;

    public partial double AspectRatioHeight { get; set; } = 2;

    public Size AspectRatio => AspectRatioMode switch
    {
        AspectRatioMode.Fixed when double.IsFinite(AspectRatioWidth)
            && double.IsFinite(AspectRatioHeight)
            && AspectRatioWidth > 0
            && AspectRatioHeight > 0
            => new Size(AspectRatioWidth, AspectRatioHeight),
        AspectRatioMode.Original => new Size(ImageSizeInPixels.Width, ImageSizeInPixels.Height),
        _ => Size.Empty
    };

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
        Register<BitmapModifiedMessage>(Receive);

        LoadImageSizeAsync().LogOnException();
    }

    private void Receive(ToggleCropImageToolMessage msg)
    {
        if (IsEnabled)
        {
            IsActive = !IsActive;
        }
    }

    private async void Receive(BitmapModifiedMessage msg)
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

    partial void OnSelectionInPixelsChanged()
    {
        OnPropertyChanged(nameof(SelectionWidthInPixels));
        OnPropertyChanged(nameof(SelectionHeightInPixels));
    }

    partial void OnAspectRatioModeChanged()
    {
        ReapplyAspectRatio();
    }

    partial void OnAspectRatioWidthChanged()
    {
        if (IsFixedAspectRatio)
        {
            ReapplyAspectRatio(AspectRatioAdjustment.Height);
        }
    }

    partial void OnAspectRatioHeightChanged()
    {
        if (IsFixedAspectRatio)
        {
            ReapplyAspectRatio(AspectRatioAdjustment.Width);
        }
    }

    private void ReapplyAspectRatio()
    {
        var aspectRatioAdjustment = AspectRatio is { IsEmpty: false } aspectRatio
            && SelectionInPixels.Width > 0
            && SelectionInPixels.Height > 0
            && aspectRatio.Width / aspectRatio.Height > SelectionInPixels.Width / (double)SelectionInPixels.Height
                ? AspectRatioAdjustment.Height
                : AspectRatioAdjustment.Width;

        ReapplyAspectRatio(aspectRatioAdjustment);
    }

    private void ReapplyAspectRatio(AspectRatioAdjustment aspectRatioAdjustment)
    {
        var previousSelection = SelectionInPixels;
        SetSelectionInPixels(previousSelection, aspectRatioAdjustment);

        if (SelectionInPixels == previousSelection)
        {
            OnPropertyChanged(nameof(SelectionInPixels));
        }
    }

    public void SetSelectionInPixels(RectInt32 selection)
    {
        SetSelectionInPixels(selection, AspectRatioAdjustment.Fit);
    }

    private void SetSelectionInPixels(RectInt32 selection, AspectRatioAdjustment aspectRatioAdjustment)
    {
        if (ImageSizeInPixels.Width < 1 || ImageSizeInPixels.Height < 1)
        {
            SelectionInPixels = default;
            return;
        }

        int x = Math.Clamp(selection.X, 0, ImageSizeInPixels.Width - 1);
        int y = Math.Clamp(selection.Y, 0, ImageSizeInPixels.Height - 1);
        int width = Math.Clamp(selection.Width, 1, ImageSizeInPixels.Width - x);
        int height = Math.Clamp(selection.Height, 1, ImageSizeInPixels.Height - y);

        if (AspectRatio is { IsEmpty: false } aspectRatioSize)
        {
            double aspectRatio = aspectRatioSize.Width / aspectRatioSize.Height;
            int maxWidth = ImageSizeInPixels.Width - x;
            int maxHeight = ImageSizeInPixels.Height - y;

            if (aspectRatioAdjustment == AspectRatioAdjustment.Width)
            {
                height = Math.Max(1, (int)Math.Round(width / aspectRatio));
                if (height > maxHeight)
                {
                    height = maxHeight;
                    width = Math.Max(1, (int)Math.Round(height * aspectRatio));
                }
            }
            else if (aspectRatioAdjustment == AspectRatioAdjustment.Height)
            {
                width = Math.Max(1, (int)Math.Round(height * aspectRatio));
                if (width > maxWidth)
                {
                    width = maxWidth;
                    height = Math.Max(1, (int)Math.Round(width / aspectRatio));
                }
            }
            else if (width / (double)height > aspectRatio)
            {
                width = Math.Clamp((int)Math.Round(height * aspectRatio), 1, width);
            }
            else
            {
                height = Math.Clamp((int)Math.Round(width / aspectRatio), 1, height);
            }
        }

        SelectionInPixels = new RectInt32(x, y, width, height);
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
        SetSelectionInPixels(new RectInt32(
            (int)Math.Round(ImageSizeInPixels.Width * 0.1d),
            (int)Math.Round(ImageSizeInPixels.Height * 0.1d),
            (int)Math.Round(ImageSizeInPixels.Width * 0.8d),
            (int)Math.Round(ImageSizeInPixels.Height * 0.8d)));
    }
}
