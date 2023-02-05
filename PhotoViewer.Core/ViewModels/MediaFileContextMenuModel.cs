using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using System.Diagnostics;

namespace PhotoViewer.Core.ViewModels;

public interface IMediaFileContextMenuModel : INotifyPropertyChanged
{
    IReadOnlyList<IMediaFileInfo> Files { get; set; }
}

public partial class MediaFileContextMenuModel : ViewModelBase, IMediaFileContextMenuModel
{
    public IReadOnlyList<IMediaFileInfo> Files { get; set; } = Array.Empty<IMediaFileInfo>();

    public bool IsOpenWithItemVisible => Files.Count == 1;

    public bool IsPrinItemVisible => false; // TODO PrintService.IsAvailable && Files.All(media => PhotoPrintJob.CanPrint(media.File));

    public bool IsSetAsItemVisible => Files.Count == 1 && personalizationService.IsFileExtensionSupported(Files.First().FileExtension);

    public bool IsSetAsDesktopBackgroundItemVisible => personalizationService.CanSetDesktopBackground;

    public bool IsSetAsLockscreenBackgroundItemVisible => personalizationService.CanSetLockScreenBackground;

    //public bool IsRenameItemVisible => Files.Count == 1 && renameCallback is not null;

    public bool IsRotateItemVisible => Files.All(file => file is IBitmapFileInfo bitmapFile && rotateBitmapService.CanRotate(bitmapFile));
    
    public bool IsShowPropertiesItemVisible => Files.Count == 1;

    public IAcceleratedCommand DeleteCommand { get; }

    private readonly IMetadataService metadataService;

    private readonly IPersonalizationService personalizationService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly IDialogService dialogService;

    private readonly IClipboardService clipboardService;

    public MediaFileContextMenuModel(
        IMessenger messenger,
        IMetadataService metadataService,
        IPersonalizationService personalizationService,
        IRotateBitmapService rotateBitmapService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IDeleteFilesCommand deleteCommand) : base(messenger)
    {
        this.metadataService = metadataService;
        this.personalizationService = personalizationService;
        this.rotateBitmapService = rotateBitmapService;
        this.dialogService = dialogService;
        this.clipboardService = clipboardService;
        DeleteCommand = deleteCommand;
    }

    [RelayCommand]
    private async Task OpenWithAsync()
    {
        await dialogService.ShowDialogAsync(new LaunchFileDialogModel(Files.First().StorageFile));
    }

    [RelayCommand]
    private void OpenInNewWindow()
    {
        var filePaths = Files.SelectMany(file => file.StorageFiles).Select(storageFile => storageFile.Path);
        Process.Start(Environment.ProcessPath!, filePaths);
    }

    [RelayCommand]
    private void Copy()
    {
        var storageFiles = Files.SelectMany(file => file.StorageFiles).ToList();
        clipboardService.CopyStorageItems(storageFiles);
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        var storageFiles = Files.SelectMany(file => file.StorageFiles).ToList();
        await dialogService.ShowDialogAsync(new ShareDialogModel(storageFiles));
    }

    [RelayCommand]
    private Task PrintAsync()
    {
        // TODO
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SetDesktopBackgroundAsync()
    {
        try
        {
            await personalizationService.SetDesktopBackgroundAsync(Files.First().StorageFile);
        }
        catch (Exception ex)
        {
            Log.Error("Could not set desktop background.", ex);
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = "", // TODO Strings.Error_SetPhotoAsDesktopBackground,
                Message = ex.Message
            });
        }
    }

    [RelayCommand]
    private async Task SetLockscreenBackgroundAsync()
    {
        try
        {
            await personalizationService.SetLockScreenBackgroundAsync(Files.First().StorageFile);
        }
        catch (Exception ex)
        {
            Log.Error("Could not set lockscreen background.", ex);
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = "",  // TODO Strings.Error_SetPhotoAsLockscreenBackground,
                Message = ex.Message
            });
        }
    }

    [RelayCommand]
    private async Task RotateAsync()
    {
        foreach (var bitmap in Files.OfType<IBitmapFileInfo>())
        {
            await rotateBitmapService.RotateClockwise90DegreesAsync(bitmap);
            Messenger.Send(new BitmapRotatedMesssage(bitmap));
        }
    }

    [RelayCommand]
    private void Rename()
    {
        // TODO
    }

    [RelayCommand]
    private async Task ShowPropertiesAsync()
    {
        var dialogModel = new PropertiesDialogModel(metadataService, Files.First());
        await dialogService.ShowDialogAsync(dialogModel);
    }

}
