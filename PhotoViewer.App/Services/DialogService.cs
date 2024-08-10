using Essentials.NET;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Printing;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace PhotoViewer.App.Services;

public class DialogService
{
    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    private readonly Window window;
    private readonly IntPtr windowHandle;

    public DialogService(Window window)
    {
        this.window = window;
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
            case LaunchFileDialogModel launchFileDialogModel:
                await ShowLaunchFileDialogAsync(launchFileDialogModel); break;
            case ShareDialogModel shareDialogModel:
                await ShowShareDialogAsync(shareDialogModel); break;
            case PrintDialogModel:
                await ShowPrintUIAsync(); break;
            default:
                await ShowCustomDialogAsync(dialogModel); break;
        }

        if (dialogModel is IViewModel viewModel)
        {
            viewModel.Cleanup();
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
        if (window.Content.XamlRoot is null)
        {
            // TODO prevent this from happening (errors on startup)
            Log.Error("XamlRoot is null");
            return;
        }

        var dialog = new ContentDialog()
        {
            Title = messageDialogModel.Title,
            Content = messageDialogModel.Message,
            CloseButtonText = messageDialogModel.CloseButtonText,
            PrimaryButtonText = messageDialogModel.PrimaryButtonText,
            SecondaryButtonText = messageDialogModel.SecondaryButtonText,
            DefaultButton = ContentDialogButton.Primary
        };
        InitializeContentDialog(dialog);
        var result = await dialog.ShowAsync();
        messageDialogModel.WasPrimaryButtonActivated = result == ContentDialogResult.Primary;
        messageDialogModel.WasSecondaryButtonActivated = result == ContentDialogResult.Secondary;
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

    private async Task ShowPrintUIAsync()
    {
        try
        {
            await PrintManagerInterop.ShowPrintUIForWindowAsync(windowHandle);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to show print UI", ex);
        }
    }

    private async Task ShowCustomDialogAsync(object dialogModel)
    {
        if (window.Content.XamlRoot is null)
        {
            // TODO prevent this from happening (errors on startup)
            Log.Error("XamlRoot is null");
            return;
        }

        var dialog = (ContentDialog)viewRegistrations.CreateViewForViewModelType(dialogModel.GetType());
        InitializeContentDialog(dialog);
        dialog.DataContext = dialogModel;
        await dialog.ShowAsync();
    }

    private void InitializeContentDialog(ContentDialog dialog)
    {
        dialog.XamlRoot = window.Content.XamlRoot;
        dialog.RequestedTheme = ((FrameworkElement)window.Content).RequestedTheme;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
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

