using System.Windows.Input;
using Windows.System;

namespace PhotoViewerCore.Utils;

public interface IAcceleratedCommand : ICommand
{
    public VirtualKey AcceleratorKey { get; }

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.None;
}
