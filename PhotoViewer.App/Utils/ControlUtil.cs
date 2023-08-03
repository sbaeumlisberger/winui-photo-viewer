using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhotoViewer.App.Utils;

internal static class ControlUtil
{
    public static long RegisterPropertyChangedCallbackSafely(this Control control, DependencyProperty property, DependencyPropertyChangedCallback propertyChangedCallback)
    {
        long token = control.RegisterPropertyChangedCallback(property, propertyChangedCallback);

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            control.Unloaded -= UserControl_Unloaded;
            control.UnregisterPropertyChangedCallback(property, token);
        }
        control.Unloaded += UserControl_Unloaded;

        return token;
    }

}
