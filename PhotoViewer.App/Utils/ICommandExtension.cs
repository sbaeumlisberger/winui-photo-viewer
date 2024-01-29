using System.Windows.Input;

namespace PhotoViewer.App.Utils;

internal static class ICommandExtension
{
    public static void TryExecute(this ICommand command, object? parameter = null)
    {
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }
}
