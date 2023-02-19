using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;
using Tocronx.SimpleAsync;

namespace PhotoViewer.App.ViewModels;

public interface IDetailsBarModel : INotifyPropertyChanged
{
    IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    bool IsVisible { get; set; }
}

public partial class DetailsBarModel : ViewModelBase, IDetailsBarModel
{
    public IMediaFlipViewItemModel? SelectedItemModel { get; set; }

    public bool IsVisible { get; set; } = false;

    public bool ShowNoInformationAvailableMessage => SelectedItemModel == null;

    public string DateFormatted { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public bool ShowColorProfileIndicator { get; private set; } = false;

    public ColorSpaceType ColorSpaceType { get; private set; } = ColorSpaceType.NotSpecified;

    public string SizeInPixels { get; private set; } = string.Empty;

    public string CameraDetails { get; private set; } = string.Empty;

    public string FileSize { get; private set; } = string.Empty;

    private readonly IMetadataService metadataService;

    private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

    public DetailsBarModel(IMessenger messenger, IMetadataService metadataService, ApplicationSettings settings) : base(messenger)
    {
        this.metadataService = metadataService;
        IsVisible = settings.AutoOpenDetailsBar;
        Messenger.Register<MetadataModifiedMessage>(this, OnReceive);
        Messenger.Register<BitmapImageLoadedMessage>(this, OnReceive);
        Messenger.Register<MediaFilesLoadingMessage>(this, OnReceive);
    }

    private void OnReceive(MetadataModifiedMessage msg)
    {
        if (msg.MetadataProperty == MetadataProperties.DateTaken
            && SelectedItemModel?.MediaItem is IBitmapFileInfo selectedFile
            && msg.Files.Contains(selectedFile))
        {
            updateRunner.RunAndCancelPrevious(async (cancellationToken) =>
            {
                var metadata = await metadataService.GetMetadataAsync(selectedFile);
                var date = metadata.Get(MetadataProperties.DateTaken) ?? (await selectedFile.GetDateModifiedAsync());
                cancellationToken.ThrowIfCancellationRequested();
                RunOnUIThread(() =>
                {
                    DateFormatted = date.ToString("g");
                });
            });
        }
    }

    private void OnReceive(BitmapImageLoadedMessage msg)
    {
        if (IsVisible && msg.BitmapFile == SelectedItemModel?.MediaItem)
        {
            UpdateFromBitmapImage(msg.BitmapImage);
        }
    }

    private async void OnReceive(MediaFilesLoadingMessage msg)
    {
        await msg.LoadMediaFilesTask.WaitForResultAsync();
        if (SelectedItemModel != null)
        {
            // linked files may have been changed
            FileName = SelectedItemModel.MediaItem.DisplayName;
        }
    }

    partial void OnSelectedItemModelChanged()
    {
        if (IsVisible)
        {
            Clear();

            if (SelectedItemModel is not null)
            {
                UpdateAsync(SelectedItemModel);
            }
        }
    }

    partial void OnIsVisibleChanged()
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
        FileName = "";
        ShowColorProfileIndicator = false;
        ColorSpaceType = ColorSpaceType.NotSpecified;
        SizeInPixels = "";
        CameraDetails = "";
        FileSize = "";
    }

    private Task UpdateAsync(IMediaFlipViewItemModel itemModel)
    {
        return updateRunner.RunAndCancelPrevious(async (cancellationToken) =>
        {
            Log.Debug($"Update details bar for {itemModel.MediaItem.DisplayName}");

            FileName = itemModel.MediaItem.DisplayName;

            if (itemModel.MediaItem is IBitmapFileInfo bitmapFile)
            {
                await UpdateFromBitmapFileAsync(bitmapFile, cancellationToken);
            }
            else
            {
                await UpdateFromMediaFileAsync(itemModel.MediaItem, cancellationToken);
            }

            if (SelectedItemModel is BitmapFlipViewItemModel bitmapFlipViewItemModel
                && bitmapFlipViewItemModel.BitmapImage is IBitmapImage bitmapImage)
            {
                UpdateFromBitmapImage(bitmapImage);
            }
        });
    }

    private void UpdateFromBitmapImage(IBitmapImage bitmapImage)
    {
        Log.Debug($"Update details bar for {SelectedItemModel!.MediaItem.DisplayName} with information from loaded image");
        ShowColorProfileIndicator = bitmapImage.ColorSpace.Profile is not null;
        ColorSpaceType = ShowColorProfileIndicator ? bitmapImage.ColorSpace.Type : ColorSpaceType.NotSpecified;
        
        // TODO video
        SizeInPixels = bitmapImage.SizeInPixels.Width + "x" + bitmapImage.SizeInPixels.Height + "px";
    }

    private async Task UpdateFromBitmapFileAsync(IBitmapFileInfo bitmapFile, CancellationToken cancellationToken)
    {
        try
        {
            if (bitmapFile.IsMetadataSupported)
            {
                var metadata = await metadataService.GetMetadataAsync(bitmapFile);
                cancellationToken.ThrowIfCancellationRequested();

                var date = metadata.Get(MetadataProperties.DateTaken) ?? (await bitmapFile.GetDateModifiedAsync());
                cancellationToken.ThrowIfCancellationRequested();
                DateFormatted = date.ToString("g");

                CameraDetails = GetCameraDetails(metadata); ;
            }
            else
            {
                var date = await bitmapFile.GetDateModifiedAsync();
                cancellationToken.ThrowIfCancellationRequested();
                DateFormatted = date.ToString("g");
            }

            ulong fileSize = await bitmapFile.GetFileSizeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            FileSize = ByteSizeFormatter.Format(fileSize);
        }
        catch (OperationCanceledException)
        {
            Log.Info($"Update details bar for {bitmapFile.DisplayName} canceled");
        }
        catch (Exception ex)
        {
            Log.Error($"Error on update details bar for {bitmapFile.DisplayName}", ex);
        }
    }

    private async Task UpdateFromMediaFileAsync(IMediaFileInfo mediaFile, CancellationToken cancellationToken)
    {
        try
        {
            var date = await mediaFile.GetDateModifiedAsync();
            cancellationToken.ThrowIfCancellationRequested();
            DateFormatted = date.ToString("g");

            ulong fileSize = await mediaFile.GetFileSizeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            FileSize = ByteSizeFormatter.Format(fileSize);
        }
        catch (OperationCanceledException)
        {
            Log.Info($"Update details bar for {mediaFile.DisplayName} canceled");
        }
        catch (Exception ex)
        {
            Log.Error($"Error on update details bar for {mediaFile.DisplayName}", ex);
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

