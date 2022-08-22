using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewerApp.Views;

public sealed partial class FlipViewPageCommandBar : CommandBar
{
    private FlipViewPageCommandBarModel ViewModel => (FlipViewPageCommandBarModel)DataContext;

    public FlipViewPageCommandBar()
    {
        this.InitializeComponent();
    }
}
