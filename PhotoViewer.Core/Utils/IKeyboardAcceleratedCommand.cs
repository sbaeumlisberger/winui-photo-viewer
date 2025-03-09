using System.Windows.Input;
using Windows.System;

namespace PhotoViewer.Core.Utils;

public interface IKeyboardAcceleratedCommand : ICommand
{
    public VirtualKey AcceleratorKey { get; }

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.None;
}
