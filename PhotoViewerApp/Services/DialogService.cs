using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerApp.Views;
using PhotoViewerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
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
}

