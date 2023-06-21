using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;

namespace PhotoViewer.App.Utils;

internal static class KeyRoutedEventArgsExtension
{
    public static bool IsModifierPressed(this KeyRoutedEventArgs e, VirtualKey modifierKey) 
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(modifierKey).HasFlag(CoreVirtualKeyStates.Down);;
    }
}
