using System;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.VisualBasic;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using Windows.Storage;
using Windows.System;

namespace PhotoViewerCore.ViewModels;

public partial class PropertiesDialogModel : ViewModelBase
{
    public string FileName { get; private set; } = string.Empty;

    public string FilePath { get; private set; } = string.Empty;

    public string FileSize { get; private set; } = string.Empty;

    public string DateTaken { get; private set; } = string.Empty;

    public string Dimensions { get; private set; } = string.Empty;

    public string Camera { get; private set; } = string.Empty;

    public string FNumber { get; private set; } = string.Empty;

    public string ShutterSpeed { get; private set; } = string.Empty;

    public string ISO { get; private set; } = string.Empty;

    public string FocalLength { get; private set; } = string.Empty;

    private bool CanShowInFileExlorer { get; set; } = false;

    private readonly IMetadataService metadataService;

    private readonly IMediaFileInfo mediaFileInfo;   

    private IStorageFolder? folder;

    public PropertiesDialogModel(IMetadataService metadataService, IMediaFileInfo mediaFileInfo)
    {
        this.metadataService = metadataService;
        this.mediaFileInfo = mediaFileInfo;
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        Log.Info($"Initialize properties dialog for {mediaFileInfo.Name}");

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

        // TODO
        //var sizeInPixels = await mediaFileInfo.GetSizeInPixelsAsync();
        //if (!sizeInPixels.IsEmpty)
        //{
        //    uint width = (uint)sizeInPixels.Width;
        //    uint height = (uint)sizeInPixels.Height;
        //    Dimensions += width + "x" + height + "px";
        //}
        //else
        //{
        //    Dimensions = "";
        //}

        if (!string.IsNullOrEmpty(mediaFileInfo.FilePath)
            && mediaFileInfo.StorageFile is StorageFile storageFile
            && await storageFile.GetParentAsync() is { } folder)
        {
            this.folder = folder;
            CanShowInFileExlorer = true;
        }
        else
        {
            CanShowInFileExlorer = false;
        }
    }

    private async Task ReadMetadataPropertiesAsync(IBitmapFileInfo mediaFileInfo)
    {
        MetadataView metadata = await metadataService.GetMetadataAsync(mediaFileInfo);

        DateTaken = metadata.Get(MetadataProperties.DateTaken)?.ToString("g", CultureInfo.InstalledUICulture) ?? "";

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
    private void ShowInFileExplorer()
    {
        var options = new FolderLauncherOptions()
        {
            ItemsToSelect = { mediaFileInfo.StorageFile }
        };
        Launcher.LaunchFolderAsync(folder, options).AsTask().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Log.Error("Could not launch folder.", task.Exception);
            }
        });
    }

}
