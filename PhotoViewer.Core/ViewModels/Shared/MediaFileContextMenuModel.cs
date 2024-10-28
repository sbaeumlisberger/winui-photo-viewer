using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Diagnostics;

namespace PhotoViewer.Core.ViewModels;

public interface IMediaFileContextMenuModel : IViewModel
{
    IReadOnlyList<IMediaFileInfo> Files { get; set; }

    public bool IsEnabled { get; set; }
}

public partial class MediaFileContextMenuModel : ViewModelBase, IMediaFileContextMenuModel
{
    public IReadOnlyList<IMediaFileInfo> Files { get; set; } = Array.Empty<IMediaFileInfo>();

    public bool IsEnabled { get; set; } = true;

    public bool IsOpenWithItemVisible => Files.Count == 1;

    public bool IsCopyPathItemVisible => Files.Count == 1;

    public bool IsPrinItemVisible => Files.All(file => file is IBitmapFileInfo);

    public bool IsSetAsItemVisible => Files.Count == 1 && personalizationService.IsFileExtensionSupported(Files.First().FileExtension);

    public bool IsSetAsDesktopBackgroundItemVisible => personalizationService.CanSetDesktopBackground;

    public bool IsSetAsLockscreenBackgroundItemVisible => personalizationService.CanSetLockScreenBackground;

    public bool IsRenameItemVisible => isRenameFilesEnabled && Files.Count == 1;

    public bool IsRotateItemVisible => Files.All(file => file is IBitmapFileInfo bitmapFile && rotateBitmapService.CanRotate(bitmapFile));

    public bool IsShowPropertiesItemVisible => Files.Count == 1;

    private readonly IMetadataService metadataService;

    private readonly IPersonalizationService personalizationService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly IDialogService dialogService;

    private readonly IClipboardService clipboardService;

    private readonly IDeleteFilesService deleteFilesService;

    private readonly bool isRenameFilesEnabled;

    internal MediaFileContextMenuModel(
        IMessenger messenger,
        IMetadataService metadataService,
        IPersonalizationService personalizationService,
        IRotateBitmapService rotateBitmapService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IDeleteFilesService deleteFilesService,
        bool isRenameFilesEnabled) : base(messenger)
    {
        this.metadataService = metadataService;
        this.personalizationService = personalizationService;
        this.rotateBitmapService = rotateBitmapService;
        this.dialogService = dialogService;
        this.clipboardService = clipboardService;
        this.deleteFilesService = deleteFilesService;
        this.isRenameFilesEnabled = isRenameFilesEnabled;
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task OpenWithAsync()
    {
        await dialogService.ShowDialogAsync(new LaunchFileDialogModel(Files.First().StorageFile));
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private void OpenInNewWindow()
    {
        var filePaths = Files.SelectMany(file => file.StorageFiles).Select(storageFile => storageFile.Path).ToList();
        Process.Start(Environment.ProcessPath!, filePaths);
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private void Copy()
    {
        if (Files.Count == 1 && Files.First() is BitmapFileInfo bitmapFile)
        {
            clipboardService.CopyBitmapFile(bitmapFile.StorageFile);
        }
        else
        {
            var storageFiles = Files.Select(file => file.StorageFile).ToList();
            clipboardService.CopyStorageItems(storageFiles);
        }
    }


    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private void CopyPath()
    {
        clipboardService.CopyText(Files.First().FilePath);
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task ShareAsync()
    {
        var storageFiles = Files.Select(file => file.StorageFile).ToList();
        await dialogService.ShowDialogAsync(new ShareDialogModel(storageFiles));
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task PrintAsync()
    {
        await dialogService.ShowDialogAsync(new PrintDialogModel());
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
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
                Title = Strings.SetDesktopBackgroundFailedDialog_Ttile,
                Message = ex.Message
            });
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
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
                Title = Strings.SetLockscreenBackgroundFailedDialog_Ttile,
                Message = ex.Message
            });
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task RotateAsync()
    {
        foreach (var bitmap in Files.OfType<IBitmapFileInfo>())
        {
            await rotateBitmapService.RotateClockwise90DegreesAsync(bitmap);
            Messenger.Send(new BitmapModifiedMesssage(bitmap));
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task DeleteAsync()
    {
        Log.Debug($"Delete [{string.Join(", ", Files.Select(file => file.DisplayName))}] via context menu");
        await deleteFilesService.DeleteFilesAsync(Files);
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private void Rename()
    {
        Messenger.Send(new ActivateRenameFileMessage(Files.First()));
    }

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task ShowPropertiesAsync()
    {
        var dialogModel = new PropertiesDialogModel(metadataService, Files.First());
        await dialogService.ShowDialogAsync(dialogModel);
    }

}
