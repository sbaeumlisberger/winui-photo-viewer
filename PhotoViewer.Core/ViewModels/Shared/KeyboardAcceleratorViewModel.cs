using Windows.System;

namespace PhotoViewer.Core.ViewModels.Shared;

public record class KeyboardAcceleratorViewModel(VirtualKeyModifiers Modifiers, VirtualKey Key);