﻿using Microsoft.UI.Xaml;
using PhotoViewerApp.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PhotoViewerApp.Utils;

public interface IDialogService
{
    Task ShowDialogAsync(object dialogModel);
}

public class DialogService : IDialogService
{
    [ThreadStatic]
    private static IDialogService? instance;

    private readonly IntPtr windowHandle;

    public DialogService(Window window)
    {
        windowHandle = WindowNative.GetWindowHandle(window);
    }

    public static IDialogService GetForCurrentWindow()
    {
        if (instance is null)
        {
            if (WindowsManger.GetForCurrentThread() is Window window)
            {
                instance = new DialogService(window);
            }
            else
            {
                throw new Exception("No window assigned to current thread.");
            }
        }
        return instance;
    }

    public async Task ShowDialogAsync(object dialogModel)
    {
        if (dialogModel is FolderPickerModel folderPickerModel)
        {
            await ShowFolderPickerAsync(folderPickerModel);
        }
    }

    public async Task ShowFolderPickerAsync(FolderPickerModel dialogModel)
    {
        var folderPicker = new FolderPicker
        {
            FileTypeFilter = { "*" },
            SuggestedStartLocation = dialogModel.SuggestedStartLocation,
        };

        InitializeWithWindow.Initialize(folderPicker, windowHandle);

        dialogModel.Folder = await folderPicker.PickSingleFolderAsync();
    }
}

