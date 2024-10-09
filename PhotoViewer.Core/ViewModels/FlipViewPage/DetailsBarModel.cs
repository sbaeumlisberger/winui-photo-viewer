using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using Windows.Foundation;

namespace PhotoViewer.Core.ViewModels;

public interface IDetailsBarModel : IViewModel
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
        Register<MetadataModifiedMessage>(OnReceive);
        Register<BitmapModifiedMesssage>(OnReceive);
        Register<BitmapImageLoadedMessage>(OnReceive);
        Register<MediaFilesLoadingMessage>(OnReceive);
        Register<MediaFilesRenamedMessage>(OnReceive);
    }

    private void OnReceive(MetadataModifiedMessage msg)
    {
        if (IsVisible
            && msg.MetadataProperty == MetadataProperties.DateTaken
            && SelectedItemModel?.MediaFile is IBitmapFileInfo selectedFile
            && msg.Files.Contains(selectedFile))
        {
            updateRunner.RunAndCancelPrevious(async (cancellationToken) =>
            {
                var metadata = await metadataService.GetMetadataAsync(selectedFile);
                var date = metadata.Get(MetadataProperties.DateTaken) ?? (await selectedFile.GetDateModifiedAsync());
                cancellationToken.ThrowIfCancellationRequested();
                DateFormatted = date.ToString("g");
            });
        }
    }

    private void OnReceive(BitmapModifiedMesssage msg)
    {
        if (IsVisible
            && SelectedItemModel?.MediaFile is IBitmapFileInfo selectedFile
            && msg.BitmapFile.Equals(selectedFile))
        {
            updateRunner.RunAndCancelPrevious(async (cancellationToken) =>
            {
                await UpdateAsync(SelectedItemModel);
            });
        }
    }

    private void OnReceive(BitmapImageLoadedMessage msg)
    {
        if (IsVisible && msg.BitmapFile == SelectedItemModel?.MediaFile)
        {
            UpdateFromBitmapImage(msg.BitmapImage);
        }
    }

    private async void OnReceive(MediaFilesLoadingMessage msg)
    {
        if (IsVisible)
        {
            await msg.LoadMediaFilesTask.WaitForResultAsync();
            if (SelectedItemModel != null)
            {
                // linked files may have been changed
                FileName = SelectedItemModel.MediaFile.DisplayName;
            }
        }
    }

    private void OnReceive(MediaFilesRenamedMessage message)
    {
        if (SelectedItemModel is not null && message.MediaFiles.Contains(SelectedItemModel.MediaFile))
        {
            FileName = SelectedItemModel.MediaFile.DisplayName;
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
            try
            {
                Log.Debug($"Update details bar for {itemModel.MediaFile.DisplayName}");

                FileName = itemModel.MediaFile.DisplayName;

                if (itemModel.MediaFile is IBitmapFileInfo bitmapFile && bitmapFile.IsMetadataSupported)
                {
                    await UpdateFromMetadataAsync(bitmapFile, cancellationToken);
                }
                else
                {
                    var date = await itemModel.MediaFile.GetDateModifiedAsync();
                    cancellationToken.ThrowIfCancellationRequested();
                    DateFormatted = date.ToString("g");
                }

                ulong fileSize = await itemModel.MediaFile.GetFileSizeAsync();
                cancellationToken.ThrowIfCancellationRequested();
                FileSize = ByteSizeFormatter.Format(fileSize);

                var sizeInPixels = await itemModel.MediaFile.GetSizeInPixelsAsync();
                cancellationToken.ThrowIfCancellationRequested();
                SizeInPixels = sizeInPixels != Size.Empty ? sizeInPixels.Width + "x" + sizeInPixels.Height + "px" : string.Empty;

                if (SelectedItemModel is IBitmapFlipViewItemModel bitmapFlipViewItemModel
                    && bitmapFlipViewItemModel.ImageViewModel.Image is IBitmapImageModel bitmapImage)
                {
                    UpdateFromBitmapImage(bitmapImage);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info($"Update details bar for {itemModel.MediaFile.DisplayName} canceled");
            }
            catch (Exception ex)
            {
                Log.Error($"Error on update details bar for {itemModel.MediaFile.DisplayName}", ex);
            }
        });
    }

    private void UpdateFromBitmapImage(IBitmapImageModel bitmapImage)
    {
        Log.Debug($"Update details bar for {SelectedItemModel!.MediaFile.DisplayName} with information from loaded image");
        ShowColorProfileIndicator = bitmapImage.ColorSpace.Profile is not null;
        ColorSpaceType = ShowColorProfileIndicator ? bitmapImage.ColorSpace.Type : ColorSpaceType.NotSpecified;
    }

    private async Task UpdateFromMetadataAsync(IBitmapFileInfo bitmapFile, CancellationToken cancellationToken)
    {
        var metadata = await metadataService.GetMetadataAsync(bitmapFile);
        cancellationToken.ThrowIfCancellationRequested();

        var date = metadata.Get(MetadataProperties.DateTaken) ?? (await bitmapFile.GetDateModifiedAsync());
        cancellationToken.ThrowIfCancellationRequested();
        DateFormatted = date.ToString("g");

        CameraDetails = GetCameraDetails(metadata);
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

