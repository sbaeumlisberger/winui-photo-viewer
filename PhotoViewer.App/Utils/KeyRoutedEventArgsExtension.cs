using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace PhotoViewer.App.Utils;

internal static class KeyRoutedEventArgsExtension
{
    public static bool IsModifierPressed(this KeyRoutedEventArgs e, VirtualKey modifierKey)
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(modifierKey).HasFlag(CoreVirtualKeyStates.Down); ;
    }
}
