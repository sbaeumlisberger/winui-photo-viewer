using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerApp.Views;
using PhotoViewerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using WinUIEx;

namespace PhotoViewerApp.Services;

public class DialogService : IDialogService
{
    private readonly WeakReference<Window> windowRef;
    private readonly IntPtr windowHandle;

    public DialogService(Window window)
    {
        windowRef = new WeakReference<Window>(window);
        windowHandle = WindowNative.GetWindowHandle(window);
    }

    public async Task ShowDialogAsync(object dialogModel)
    {
        switch (dialogModel)
        {
            case FolderPickerModel folderPickerModel:
                await ShowFolderPickerAsync(folderPickerModel); break;
            case FileOpenPickerModel fileOpenPickerModel:
                await ShowFileOpenPickerModelAsync(fileOpenPickerModel); break;
            case FileSavePickerModel fileSavePickerModel:
                await ShowFileSavePickerModelAsync(fileSavePickerModel); break;
            case MessageDialogModel messageDialogModel:
                await ShowMessageDialogAsync(messageDialogModel); break;
            case YesNoDialogModel yesNoDialogModel:
                await ShowYesNoDialogAsync(yesNoDialogModel); break;
            case LaunchFileDialogModel launchFileDialogModel:
                await ShowLaunchFileDialogAsync(launchFileDialogModel); break;
            case ShareDialogModel shareDialogModel:
                await ShowShareDialogAsync(shareDialogModel); break;
            default:
                throw new Exception();
        }
    }

    private async Task ShowFolderPickerAsync(FolderPickerModel dialogModel)
    {
        var folderPicker = new FolderPicker
        {
            FileTypeFilter = { "*" },
            SuggestedStartLocation = dialogModel.SuggestedStartLocation,
        };

        InitializeWithWindow.Initialize(folderPicker, windowHandle);

        dialogModel.Folder = await folderPicker.PickSingleFolderAsync();
    }

    private async Task ShowFileOpenPickerModelAsync(FileOpenPickerModel dialogModel)
    {
        var fileOpenPicker = new FileOpenPicker();

        if (dialogModel.FileTypeFilter is IList<string> fileTypeFilter)
        {
            fileOpenPicker.FileTypeFilter.AddRange(fileTypeFilter);
        }
        else
        {
            fileOpenPicker.FileTypeFilter.Add("*");
        }

        InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

        dialogModel.File = await fileOpenPicker.PickSingleFileAsync();
    }

    private async Task ShowFileSavePickerModelAsync(FileSavePickerModel dialogModel)
    {
        if (dialogModel.FileTypeChoices is null)
        {
            throw new ArgumentException("The property FileTypeChoices must be set", nameof(dialogModel));
        }

        var fileSavePicker = new FileSavePicker
        {
            SuggestedFileName = dialogModel.SuggestedFileName,
        };

        dialogModel.FileTypeChoices.ForEach(ftc => fileSavePicker.FileTypeChoices.Add(ftc));

        InitializeWithWindow.Initialize(fileSavePicker, windowHandle);

        dialogModel.File = await fileSavePicker.PickSaveFileAsync();
    }

    private async Task ShowMessageDialogAsync(MessageDialogModel messageDialogModel)
    {
        if (windowRef.TryGetTarget(out var window))
        {
            var dialog = new ContentDialog()
            {
                Title = messageDialogModel.Title,
                Content = messageDialogModel.Message
            };
            dialog.XamlRoot = window.Content.XamlRoot;
            await dialog.ShowAsync();
        }
    }

    private async Task ShowYesNoDialogAsync(YesNoDialogModel yesNoDialogModel)
    {
        if (windowRef.TryGetTarget(out var window))
        {
            var dialog = new YesNoDialog(
                yesNoDialogModel.Title,
                yesNoDialogModel.Message,
                yesNoDialogModel.RememberMessage);
            dialog.XamlRoot = window.Content.XamlRoot;
            yesNoDialogModel.IsYes = await dialog.ShowAsync();
            yesNoDialogModel.IsRemember = dialog.IsRemember;
        }
    }

    private async Task ShowLaunchFileDialogAsync(LaunchFileDialogModel launchFileDialogModel)
    {
        var options = new LauncherOptions
        {
            DisplayApplicationPicker = true
        };
        await Launcher.LaunchFileAsync(launchFileDialogModel.File, options);
    }

    private Task ShowShareDialogAsync(ShareDialogModel shareDialogModel)
    {
        var dataTransferManager = GetDataTransferManagerForWindow(windowHandle);
        dataTransferManager.DataRequested += DataTransferManager_DataRequested;
        void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetStorageItems(shareDialogModel.Files);
            dataPackage.Properties.Title = string.Join(", ", shareDialogModel.Files.Select(file => file.Name));
            args.Request.Data = dataPackage;
            dataTransferManager.DataRequested -= DataTransferManager_DataRequested;
        }
        DataTransferManager.As<IDataTransferManagerInterop>().ShowShareUIForWindow(windowHandle);
        return Task.CompletedTask;
    }

    private static DataTransferManager GetDataTransferManagerForWindow([In] IntPtr appWindow)
    {
        IDataTransferManagerInterop interop = DataTransferManager.As<IDataTransferManagerInterop>();
        Guid riid = new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);
        return DataTransferManager.FromAbi(interop.GetForWindow(appWindow, riid));
    }

    [ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
        void ShowShareUIForWindow(IntPtr appWindow);
    }

}

