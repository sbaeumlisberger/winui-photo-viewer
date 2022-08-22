using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewerApp.Views;
public sealed partial class MediaFlipViewItem : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty<MediaFlipViewItem>.Register(nameof(ViewModel), typeof(MediaFlipViewItemModel), null, (obj, args) => obj.OnViewModelChanged());

    public MediaFlipViewItemModel? ViewModel { get => (MediaFlipViewItemModel?)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }

    public MediaFlipViewItem()
    {
        this.InitializeComponent();
    }

    private void OnViewModelChanged() 
    {
        this.Bindings.Update();
    }
}
