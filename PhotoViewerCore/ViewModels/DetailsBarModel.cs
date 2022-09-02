using CommunityToolkit.Mvvm.ComponentModel;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using System.ComponentModel;

namespace PhotoViewerApp.ViewModels;

public interface IDetailsBarModel : INotifyPropertyChanged
{
    IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    bool IsVisible { get; set; }
}

public partial class DetailsBarModel : ViewModelBase, IDetailsBarModel
{
    [ObservableProperty]
    private IMediaFlipViewItemModel? selectedItemModel;

    [ObservableProperty]
    private bool isVisible = true;

    [ObservableProperty]
    private string dateFormatted = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private bool showColorProfileIndicator = false;

    [ObservableProperty]
    private ColorSpaceType colorSpaceType = ColorSpaceType.NotSpecified;

    [ObservableProperty]
    private string sizeInPixels = string.Empty;

    [ObservableProperty]
    private string cameraDetails = string.Empty;

    [ObservableProperty]
    private string fileSize = string.Empty;

    private readonly IMetadataService metadataService;

    public DetailsBarModel(IMetadataService metadataService)
    {
        this.metadataService = metadataService;
    }

    partial void OnSelectedItemModelChanged(IMediaFlipViewItemModel? value)
    {
        if (IsVisible)
        {
            Clear();

            if (SelectedItemModel is not null)
            {
                UpdateAsync(SelectedItemModel); // TODO cancel/wait previous update!
            }
        }
    }

    partial void OnIsVisibleChanged(bool value)
    {
        if (IsVisible)
        {
            if (SelectedItemModel is not null)
            {
                UpdateAsync(SelectedItemModel);
            }
        }
        else
        {
            Clear();
        }
    }

    private void Clear()
    {
        DateFormatted = "";
        FileName = "";// Strings.DetailsBar_NoInformationAvailable;
        ShowColorProfileIndicator = false;
        ColorSpaceType = ColorSpaceType.NotSpecified;
        SizeInPixels = "";
        CameraDetails = "";
        FileSize = "";
    }

    private async void UpdateAsync(IMediaFlipViewItemModel itemModel)
    {
        Log.Debug($"Update details bar for {itemModel.MediaItem.Name}");

        FileName = itemModel.MediaItem.Name;

        if (itemModel.MediaItem is IBitmapFileInfo bitmapFile)
        {
            await UpdateFromBitmapFileAsync(bitmapFile);
        }
        else
        {
            await UpdateFromMediaFileAsync(itemModel.MediaItem);
        }

        if (await itemModel.WaitUntilImageLoaded() is IBitmapImage bitmapImage)
        {
            ShowColorProfileIndicator = bitmapImage.ColorSpace.Profile is not null;
            ColorSpaceType = ShowColorProfileIndicator ? bitmapImage.ColorSpace.Type : ColorSpaceType.NotSpecified;
            SizeInPixels = bitmapImage.SizeInPixels.Width + "x" + bitmapImage.SizeInPixels.Height + "px";     
        }
    }

    private async Task UpdateFromBitmapFileAsync(IBitmapFileInfo bitmapFile)
    {
        try
        {
            if (bitmapFile.IsMetadataSupported)
            {
                var metadata = await metadataService.GetMetadataAsync(bitmapFile);

                var date = metadata.Get(MetadataProperties.DateTaken) ?? (await bitmapFile.GetDateModifiedAsync());
                DateFormatted = date.ToString("g");

                CameraDetails = GetCameraDetails(metadata);;
            }
            else 
            {
                var date = await bitmapFile.GetDateModifiedAsync();
                DateFormatted = date.ToString("g");        
            }

            ulong fileSize = await bitmapFile.GetFileSizeAsync();
            FileSize = ByteSizeFormatter.Format(fileSize);
        }
        catch (Exception ex)
        {
            Log.Error("Error on update details bar", ex);
        }
    }

    private async Task UpdateFromMediaFileAsync(IMediaFileInfo mediaItem)
    {
        try
        {
            var date = await mediaItem.GetDateModifiedAsync();
            DateFormatted = date.ToString("g");

            ulong fileSize = await mediaItem.GetFileSizeAsync();
            FileSize = ByteSizeFormatter.Format(fileSize);
        }
        catch (Exception ex)
        {
            Log.Error("Error on update details bar", ex);       
        }
    }

    private string GetCameraDetails(MetadataView metadata)
    {
        string cameraDetails = "";

        if (metadata.Get(MetadataProperties.ExposureTime) is Fraction exposureTime)
        {
            cameraDetails += exposureTime.GetReduced().ToString() + "s ";
        }

        if (metadata.Get(MetadataProperties.FNumber) is Fraction fNumber)
        {
            cameraDetails += "F" + fNumber.ToDouble().ToString("#.#") + " ";
        }

        ushort? isoSpeed = metadata.Get(MetadataProperties.ISOSpeed);
        if (isoSpeed != null)
        {
            cameraDetails += "ISO" + isoSpeed + " ";
        }

        double? focalLength = metadata.Get(MetadataProperties.FocalLength);
        ushort? focalLengthInFilm = metadata.Get(MetadataProperties.FocalLengthInFilm);
        if (focalLength != null && focalLengthInFilm != null)
        {
            cameraDetails += focalLength + "(" + focalLengthInFilm + ")mm";
        }

        return cameraDetails;
    }

}

