using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Printing;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using WinRT.Interop;

namespace PhotoViewer.App.Services;

public class DialogService
{
    private static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");

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
            case FileOpenPickerModel2 fileOpenPickerModel:
                ShowFileOpenPickerModel(fileOpenPickerModel); break;
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

    private unsafe void ShowFileOpenPickerModel(FileOpenPickerModel2 dialogModel)
    {
        PInvoke.CoCreateInstance(CLSID_FileOpenDialog, null, CLSCTX.CLSCTX_INPROC_SERVER, out IFileOpenDialog* fileOpenDialog).ThrowOnFailure(); ;

        if (dialogModel.InitialFolder is { } initialFolder)
        {
            PInvoke.SHCreateItemFromParsingName(initialFolder, null, typeof(IShellItem).GUID, out void* initialFolderShellItem).ThrowOnFailure();
            fileOpenDialog->SetFolder((IShellItem*)initialFolderShellItem);
        }

        if (dialogModel.FileTypeFilter is { } fileTypeFilter)
        {
            string filterName = Strings.FileOpenDialog_AllFiles;
            string filterPattern = string.Join(";", fileTypeFilter.Select(fileType => "*" + fileType));

            fixed (char* pfilterName = filterName, pFilterPattern = filterPattern)
            {
                var filterSpec = new COMDLG_FILTERSPEC
                {
                    pszName = new PCWSTR(pfilterName),
                    pszSpec = new PCWSTR(pFilterPattern),
                };
                fileOpenDialog->SetFileTypes(1, &filterSpec);
            }
        }

        try
        {
            fileOpenDialog->Show(new HWND(windowHandle));
            IShellItem* shellItem = null;
            fileOpenDialog->GetResult(&shellItem);
            PWSTR filePathPWSTR = null;
            shellItem->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &filePathPWSTR);
            dialogModel.FilePath = new string(filePathPWSTR);
            PInvoke.CoTaskMemFree(filePathPWSTR);
        }
        catch (COMException e) when (e.HResult == unchecked((int)0x800704C7))
        {
            // cancelled
        }
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
        var dataTransferManager = DataTransferManagerInterop.GetForWindow(windowHandle);
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
        DataTransferManagerInterop.ShowShareUIForWindow(windowHandle);
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

}

