using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;

namespace PhotoViewerApp.Utils;

public static class EnterKeyExtension
{

    public static readonly DependencyProperty CommandProperty = DependencyPropertyHelper.RegisterAttached<ICommand?>(
        typeof(EnterKeyExtension), nameof(CommandProperty), null, OnCommandChanged);

    public static ICommand GetCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(CommandProperty, value);
    }

    private static void OnCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        UIElement uiElement = (UIElement)obj;

        if (e.NewValue != null && e.OldValue == null)
        {
            uiElement.PreviewKeyDown += UIElement_KeyDown;
        }
        else if (e.NewValue == null)
        {
            uiElement.PreviewKeyDown -= UIElement_KeyDown;
        }
    }

    private static void UIElement_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ICommand command = GetCommand((DependencyObject)sender);

            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }

}