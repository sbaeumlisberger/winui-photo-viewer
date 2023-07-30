using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;

namespace PhotoViewer.App.Utils;

public class EnterKeyExtension
{

    public static readonly DependencyProperty CommandProperty = DependencyPropertyHelper<EnterKeyExtension>
        .RegisterAttached<ICommand?>(null, OnCommandChanged);

    private static readonly KeyEventHandler KeyDownEventHandler = new KeyEventHandler(UIElement_KeyDown);

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
            uiElement.AddHandler(UIElement.KeyDownEvent, KeyDownEventHandler, true);
        }
        else if (e.NewValue == null)
        {
            uiElement.RemoveHandler(UIElement.KeyDownEvent, KeyDownEventHandler);
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

            e.Handled = true;
        }
    }

}