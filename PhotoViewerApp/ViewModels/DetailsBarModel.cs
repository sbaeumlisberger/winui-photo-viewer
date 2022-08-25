﻿using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewerApp.Models;
using PhotoViewerApp.Resources;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCoreModule.Model;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PhotoViewerApp.ViewModels;

public interface IDetailsBarModel : INotifyPropertyChanged
{
    IMediaFlipViewItemModel? SelectedItemModel { get; set; }
}

public partial class DetailsBarModel : ViewModelBase, IDetailsBarModel
{
    [ObservableProperty]
    private IMediaFlipViewItemModel? selectedItemModel;

    [ObservableProperty]
    private bool isVisible = true;

    [ObservableProperty]
    private string textLeft = string.Empty;

    [ObservableProperty]
    private string textCenter = Strings.DetailsBar_NoInformationAvailable;

    [ObservableProperty]
    private string textRight = string.Empty;

    [ObservableProperty]
    private bool showColorProfileIndicator = false;

    [ObservableProperty]
    private string colorSpaceName = string.Empty;

    partial void OnSelectedItemModelChanged(IMediaFlipViewItemModel? value)
    {
        if (IsVisible)
        {
            if (SelectedItemModel is not null)
            {
                UpdateAsync(SelectedItemModel); // TODO cancel/wait previous update!
            }
            else
            {
                Clear();
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
        TextLeft = "";
        TextCenter = Strings.DetailsBar_NoInformationAvailable;
        TextRight = "";
    }

    private async void UpdateAsync(IMediaFlipViewItemModel itemModel)
    {
        Log.Debug($"Update details bar for {itemModel.MediaItem.Name}.");

        TextCenter = itemModel.MediaItem.Name;

        if (itemModel.MediaItem is BitmapGraphicItem bitmapGraphicItem && bitmapGraphicItem.IsMetadataSupported)
        {
            await UpdateFromBitmapGraphicItemAsync(bitmapGraphicItem);
        }
        else
        {
            await UpdateFromMediaItemAsync(itemModel.MediaItem);
        }

        if (await itemModel.WaitUntilImageLoaded() is IBitmapImage bitmapImage)
        {
            ShowColorProfileIndicator = bitmapImage.ColorSpace.Profile is not null;
            ColorSpaceName = ShowColorProfileIndicator ? ConvertColorSpaceTypeToDisplayName(bitmapImage.ColorSpace.Type) : string.Empty;
        }
    }

    private async Task UpdateFromBitmapGraphicItemAsync(BitmapGraphicItem bitmapGraphicItem)
    {
        //try
        //{
        //    var metadata = await photo.GetMetadataAsync();

        //    var date = metadata.Get(MetadataProperties.DateTaken) ?? (await photo.File.GetBasicPropertiesAsync()).DateModified;
        //    ulong fileSize = await photo.GetFileSizeAsync();
        //    DateTakenAndSize = date.ToString("g", CultureInfo.InstalledUICulture) + " " + ByteSizeFormatter.Format(fileSize);

        //    string cameraDetails = "";

        //    var sizeInPixels = await photo.GetSizeInPixelsAsync();
        //    if (!sizeInPixels.IsEmpty)
        //    {
        //        uint width = (uint)sizeInPixels.Width;
        //        uint height = (uint)sizeInPixels.Height;
        //        cameraDetails += width + "x" + height + "px ";
        //    }

        //    if (metadata.Get(MetadataProperties.ExposureTime) is Fraction exposureTime)
        //    {
        //        cameraDetails += exposureTime.GetReduced().ToString() + "s ";
        //    }

        //    if (metadata.Get(MetadataProperties.FNumber) is Fraction fNumber)
        //    {
        //        cameraDetails += "F" + fNumber.ToDouble().ToString("#.#") + " ";
        //    }

        //    ushort? isoSpeed = metadata.Get(MetadataProperties.ISOSpeed);
        //    if (isoSpeed != null)
        //    {
        //        cameraDetails += "ISO" + isoSpeed + " ";
        //    }

        //    double? focalLength = metadata.Get(MetadataProperties.FocalLength);
        //    ushort? focalLengthInFilm = metadata.Get(MetadataProperties.FocalLengthInFilm);
        //    if (focalLength != null && focalLengthInFilm != null)
        //    {
        //        cameraDetails += focalLength + "(" + focalLengthInFilm + ")mm";
        //    }

        //    CameraDetails = cameraDetails;
        //}
        //catch (Exception ex)
        //{
        //    Log.Error("Could not load metadata", ex);
        //    DateTakenAndSize = await TryGetDateModifiedAndSizeAsync(photo);
        //    CameraDetails = "";
        //}
    }

    private async Task UpdateFromMediaItemAsync(IMediaItem mediaItem)
    {
        try
        {
            var dateModified = await mediaItem.GetDateModifiedAsync();
            TextLeft = dateModified.ToString("g");
        }
        catch (Exception ex)
        {
            Log.Error("Could not retrieve date modified.", ex);
            TextLeft = string.Empty;
        }

        try
        {
            //var sizeInPixels = await mediaItem.GetSizeInPixelsAsync();
            //if (sizeInPixels != Size.Empty)
            //{
            //    TextRight = sizeInPixels.Width + "x" + sizeInPixels.Height + "px";
            //}
            //else
            //{
            //    TextRight = "";
            //}

            ulong fileSize = await mediaItem.GetFileSizeAsync();
            TextRight = ByteSizeFormatter.Format(fileSize);
        }
        catch (Exception ex)
        {
            Log.Error("Could not retrieve file size or resolution.", ex);
            TextRight = string.Empty;
        }
    }

    private string ConvertColorSpaceTypeToDisplayName(ColorSpaceType colorSpaceType)
    {
        switch (colorSpaceType)
        {
            case ColorSpaceType.SRGB:
                return Strings.DetailsBar_ColorSpaceSRGB;
            case ColorSpaceType.AdobeRGB:
                return Strings.DetailsBar_ColorSpaceAdobeRGB;
            default:
                return Strings.DetailsBar_ColorSpaceUnknown;
        }
    }

}

