using Microsoft.UI.Windowing;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;

namespace PeopleTagTool.Services;

public class DialogService(Func<AppWindow> retrieveWindow)
{
    public async Task<string?> PickFolderAsync()
    {
        FolderPicker folderPicker = new FolderPicker(retrieveWindow().Id);
        var result = await folderPicker.PickSingleFolderAsync();
        return result?.Path;
    }
}
