using CommunityToolkit.Mvvm.Input;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Globalization;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.Core.ViewModels;

public partial class PropertiesDialogModel : ViewModelBase
{
    public string FileName { get; private set; } = string.Empty;

    public string FilePath { get; private set; } = string.Empty;

    public string FileSize { get; private set; } = string.Empty;

    public string DateTaken { get; private set; } = string.Empty;

    public string Dimensions { get; private set; } = string.Empty;

    public string Orientation { get; private set; } = string.Empty;

    public string Camera { get; private set; } = string.Empty;

    public string FNumber { get; private set; } = string.Empty;

    public string ShutterSpeed { get; private set; } = string.Empty;

    public string ISO { get; private set; } = string.Empty;

    public string FocalLength { get; private set; } = string.Empty;

    private bool CanShowInFileExlorer { get; set; } = false;

    private readonly IMetadataService metadataService;

    private readonly IMediaFileInfo mediaFileInfo;

    public PropertiesDialogModel(IMetadataService metadataService, IMediaFileInfo mediaFileInfo)
    {
        this.metadataService = metadataService;
        this.mediaFileInfo = mediaFileInfo;
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        Log.Info($"Initialize properties dialog for {mediaFileInfo.DisplayName}");

        FileName = mediaFileInfo.FileName;

        ulong fileSizeInBytes = await mediaFileInfo.GetFileSizeAsync();
        FileSize = ByteSizeFormatter.Format(fileSizeInBytes);

        FilePath = mediaFileInfo.FilePath;

        if (mediaFileInfo is IBitmapFileInfo bitmapFileInfo && bitmapFileInfo.IsMetadataSupported)
        {
            try
            {
                await ReadMetadataPropertiesAsync(bitmapFileInfo);
            }
            catch
            {
                ClearMetadataProperties();
            }
        }
        else
        {
            ClearMetadataProperties();
        }

        var sizeInPixels = await mediaFileInfo.GetSizeInPixelsAsync();
        if (!sizeInPixels.IsEmpty)
        {
            uint width = (uint)sizeInPixels.Width;
            uint height = (uint)sizeInPixels.Height;
            Dimensions += width + "x" + height + "px";
        }
        else
        {
            Dimensions = "";
        }

        CanShowInFileExlorer = Path.GetDirectoryName(mediaFileInfo.FilePath) != null;
    }

    private async Task ReadMetadataPropertiesAsync(IBitmapFileInfo mediaFileInfo)
    {
        MetadataView metadata = await metadataService.GetMetadataAsync(mediaFileInfo);

        DateTaken = metadata.Get(MetadataProperties.DateTaken)?.ToString("g", CultureInfo.InstalledUICulture) ?? "";

        var orientation = metadata.Get(MetadataProperties.Orientation);
        Orientation = $"{(int)orientation} ({MapOrientationToDisplayText(orientation)})";

        string cameraManufacturer = metadata.Get(MetadataProperties.CameraManufacturer) ?? "";
        string cameraModel = metadata.Get(MetadataProperties.CameraModel) ?? "";
        Camera = cameraManufacturer + " " + cameraModel;

        ShutterSpeed = metadata.Get(MetadataProperties.ExposureTime) is Fraction exposureTime ? exposureTime.ToString() + "s" : "";

        FNumber = metadata.Get(MetadataProperties.FNumber) is Fraction fNumber ? "F" + fNumber.ToDouble().ToString("#.#") : "";

        ISO = metadata.Get(MetadataProperties.ISOSpeed)?.ToString() ?? "";

        var focalLength = metadata.Get(MetadataProperties.FocalLength);
        FocalLength = focalLength != null ? focalLength.ToString() + "mm" : "";
    }

    private void ClearMetadataProperties()
    {
        DateTaken = "";
        Dimensions = "";
        Camera = "";
        ShutterSpeed = "";
        FNumber = "";
        ISO = "";
        FocalLength = "";
    }

    [RelayCommand(CanExecute = nameof(CanShowInFileExlorer))]
    private async Task ShowInFileExplorer()
    {
        var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(mediaFileInfo.FilePath)!);

        var options = new FolderLauncherOptions()
        {
            ItemsToSelect = { mediaFileInfo.StorageFile }
        };
        await Launcher.LaunchFolderAsync(folder, options).AsTask().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Log.Error("Could not launch folder.", task.Exception);
            }
        });
    }

    private string MapOrientationToDisplayText(PhotoOrientation orientation)
    {
        switch (orientation)
        {
            case PhotoOrientation.Unspecified:
                return Strings.PhotoOrientation_Unspecified;
            case PhotoOrientation.Normal:
                return Strings.PhotoOrientation_Normal;
            case PhotoOrientation.Rotate90:
                return Strings.PhotoOrientation_Rotate90;
            case PhotoOrientation.Rotate180:
                return Strings.PhotoOrientation_Rotate180;
            case PhotoOrientation.Rotate270:
                return Strings.PhotoOrientation_Rotate270;
            default:
                return orientation.ToString();
        }
    }
}
