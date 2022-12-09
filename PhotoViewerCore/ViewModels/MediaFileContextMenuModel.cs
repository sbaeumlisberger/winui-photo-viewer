using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Commands;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System.ComponentModel;

namespace PhotoViewerCore.ViewModels;

public interface IMediaFileContextMenuModel : INotifyPropertyChanged
{
    IReadOnlyList<IMediaFileInfo> Files { get; set; }
}

public partial class MediaFileContextMenuModel : ViewModelBase, IMediaFileContextMenuModel
{
    public IReadOnlyList<IMediaFileInfo> Files { get; set; } = Array.Empty<IMediaFileInfo>();

    [DependsOn(nameof(Files))]
    public bool IsOpenWithItemVisible => Files.Count == 1;

    public bool IsPrinItemVisible => false; // TODO PrintService.IsAvailable && Files.All(media => PhotoPrintJob.CanPrint(media.File));

    public bool IsSetAsItemVisible => Files.Count == 1 && personalizationService.IsFileTypeSupported(Files.First().StorageFile);

    public bool IsSetAsDesktopBackgroundItemVisible => personalizationService.CanSetDesktopBackground;

    public bool IsSetAsLockscreenBackgroundItemVisible => personalizationService.CanSetLockScreenBackground;

    //public bool IsRenameItemVisible => Files.Count == 1 && renameCallback is not null;

    public bool IsRotateItemVisible => Files.All(file => file is IBitmapFileInfo bitmapFile && rotateBitmapService.CanRotate(bitmapFile));

    public bool IsShowPropertiesItemVisible => Files.Count == 1;

    public IAcceleratedCommand DeleteCommand { get; }

    private readonly IPersonalizationService personalizationService;

    private readonly IRotateBitmapService rotateBitmapService;

    private readonly IDialogService dialogService;

    private readonly IClipboardService clipboardService;

    //private readonly IViewModelFactory viewModelFactory;
    //private readonly Action<IMedia>? renameCallback;

    public MediaFileContextMenuModel(
        IMessenger messenger,
        IPersonalizationService personalizationService,
        IRotateBitmapService rotateBitmapService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IDeleteFilesCommand deleteCommand) : base(messenger)
    {
        this.personalizationService = personalizationService;
        this.rotateBitmapService = rotateBitmapService;
        this.dialogService = dialogService;
        this.clipboardService = clipboardService;
        DeleteCommand = deleteCommand;

        //OpenWithCommand = new AsyncCommand(OpenWithAsync);
        //OpenInNewWindowCommand = new AsyncCommand(OpenInNewWindowAsync);
        //CopyCommand = new Command(Copy);
        //ShareCommand = new SharePhotoCommand(() => Files);
        //PrintCommand = new AsyncCommand(PrintAsync);
        //SetDesktopBackgroundCommand = new AsyncCommand(SetDesktopBackgroundAsync);
        //SetLockscreenBackgroundCommand = new AsyncCommand(SetLockscreenBackgroundAsync);
        //SetAppTileBackgroundCommand = new AsyncCommand(SetAppTileBackgroundAsync);
        //RenameCommand = new Command(Rename);
        //RotateCommand = new RotateCommand(/*TODO*/new RotatePhotoService(), messenger, () => Files.OfType<IPhoto>());
        //DeleteCommand = new DeleteCommand(/*TODO*/new DeleteService(), new ProcessingService(), messenger, () => Files);
        //ShowDetailsCommand = new Command(ShowDetails);
    }

    [RelayCommand]
    private async Task OpenWithAsync()
    {
        await dialogService.ShowDialogAsync(new LaunchFileDialogModel(Files.First().StorageFile));
    }

    [RelayCommand]
    private async Task OpenInNewWindowAsync()
    {
        //var mediaSource = new MultipleFilesPhotoSource(Files.SelectMany(photo => photo.Files).ToList());
        //var loadMediaTask = new PhotoLoadService().LoadPhotosAsync(mediaSource, PhotoViewerSettings.PhotoLoadingConfig);
        //await ShowWindowAsync(() =>
        //{
        //    var frameModel = viewModelFactory.CreateFrameModel();
        //    MessengerProvider.Messenger.Publish(new MediaLoadingMessage(loadMediaTask));
        //    return frameModel;
        //});
    }

    [RelayCommand]
    private void Copy()
    {
        var storageFiles = Files.SelectMany(file => new[] { file.StorageFile }.Concat(file.LinkedStorageFiles)).ToList();
        clipboardService.CopyStorageItems(storageFiles);
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        var storageFiles = Files.SelectMany(file => new[] { file.StorageFile }.Concat(file.LinkedStorageFiles)).ToList();
        await dialogService.ShowDialogAsync(new ShareDialogModel(storageFiles));
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        //var printJob = new PhotoPrintJob(Files.Select(photo => photo.File));
        //await PrintService.Current.ShowPrintUIAsync(printJob);
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
                Title = "",//Strings.Error_SetPhotoAsDesktopBackground,
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
                Title = "",//Strings.Error_SetPhotoAsLockscreenBackground,
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
        //renameCallback!.Invoke(Files.First());
    }

    [RelayCommand]
    private void ShowProperties()
    {
        //ShowDialogAsync(new DetailsDialogModel(Files.First()));
    }

}
