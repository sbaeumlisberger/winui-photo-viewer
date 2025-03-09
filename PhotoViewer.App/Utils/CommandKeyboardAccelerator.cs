using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace PhotoViewer.App.Utils;

public class CommandKeyboardAccelerator : KeyboardAccelerator
{
    public static readonly DependencyProperty CommandProperty = DependencyPropertyHelper<CommandKeyboardAccelerator>.Register<ICommand?>(nameof(Command), null);

    public static readonly DependencyProperty CommandParameterProperty = DependencyPropertyHelper<CommandKeyboardAccelerator>.Register<object?>(nameof(CommandParameter), null);

    public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public CommandKeyboardAccelerator()
    {
        Invoked += OnKeyboardAcceleratornvoked;
    }

    private void OnKeyboardAcceleratornvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (Command is not null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
            args.Handled = true;
        }
    }
}
