using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace PhotoViewerApp.Utils;

internal static class UserControlUtil
{

    public static void WhenDataContextSet(this UserControl userControl, Action action)
    {
        void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (userControl.DataContext != null)
            {
                action();
                userControl.DataContextChanged -= UserControl_DataContextChanged;
            }         
        }
        userControl.DataContextChanged += UserControl_DataContextChanged;
    }

    public static void RegisterPropertyChangedCallbackSafely(this UserControl userControl, DependencyProperty property, DependencyPropertyChangedCallback propertyChangedCallback)
    {
        long token = userControl.RegisterPropertyChangedCallback(property, propertyChangedCallback);

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            userControl.Unloaded -= UserControl_Unloaded;
            userControl.UnregisterPropertyChangedCallback(property, token);
        }
        userControl.Unloaded += UserControl_Unloaded;       
    }


}
