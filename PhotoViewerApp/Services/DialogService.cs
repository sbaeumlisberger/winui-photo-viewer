using Microsoft.UI.Xaml;
using PhotoViewerApp.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.Services;

public class DialogService : IDialogService
{
    public static IDialogService Instance => Instance ?? throw new InvalidOperationException("DialogService is not initialized.");

    private readonly IntPtr windowHandle;

    public DialogService(Window window)
    {
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
}

