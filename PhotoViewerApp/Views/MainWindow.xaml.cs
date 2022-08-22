using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerApp.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewerApp
{
    public sealed partial class MainWindow : Window
    {
        private readonly Guid id = Guid.NewGuid();

        public MainWindow()
        {
            WindowsManger.SetForCurrentThread(this);  
            this.InitializeComponent();
            Activated += MainWindow_Activated;
            frame.Navigate(typeof(FlipViewPage));            
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                Log.Info($"Window {id} activated.");
            }
        }
    }
}
