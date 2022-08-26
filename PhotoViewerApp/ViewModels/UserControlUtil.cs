using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace PhotoViewerApp.ViewModels;

internal static class UserControlUtil
{

    public static void WhenDataContextSet(this UserControl userControl, Action action) 
    {
        void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (userControl.DataContext != null) 
            {
                action();
            }
            userControl.DataContextChanged -= UserControl_DataContextChanged;
        }
        userControl.DataContextChanged += UserControl_DataContextChanged;
    }

  
}
