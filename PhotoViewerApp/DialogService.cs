using Microsoft.UI.Xaml;
using PhotoViewerApp.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

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

