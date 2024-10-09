using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels.Shared;

namespace PhotoViewer.App.Views.Shared;

public sealed partial class ToolsAppBarButton : AppBarButton, IMVVMControl<ToolsMenuModel>
{
    public ToolsAppBarButton()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(ToolsMenuModel viewModel)
    {
        // register all keyboard accelerators on the button,
        // because in the menu flyout they are not working

        IAcceleratedCommand[] commands = [
            viewModel.MoveRawFilesToSubfolderCommand,
            viewModel.DeleteSingleRawFilesCommand,
            viewModel.ShiftDatenTakenCommand,
            viewModel.ImportGpxTrackCommand,
            viewModel.PrefixFilesByDateCommand];

        foreach (var command in commands)
        {
            var keyboardAccelerator = new KeyboardAccelerator()
            {
                Key = command.AcceleratorKey,
                Modifiers = command.AcceleratorModifiers,
            };
            keyboardAccelerator.Invoked += (sender, args) => command.Execute(null);
            KeyboardAccelerators.Add(keyboardAccelerator);
        }

        KeyboardAcceleratorTextOverride = " ";
    }
}
