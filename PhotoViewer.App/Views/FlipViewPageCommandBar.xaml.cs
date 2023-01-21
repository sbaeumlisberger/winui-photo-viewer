using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System.Collections.Generic;

namespace PhotoViewerApp.Views;

public sealed partial class FlipViewPageCommandBar : CommandBar
{
    private FlipViewPageCommandBarModel ViewModel => (FlipViewPageCommandBarModel)DataContext;

    public FlipViewPageCommandBar()
    {
        this.InitializeComponent();
    }

    private List<IMediaFileInfo> ListOf(IMediaFileInfo element) 
    {
        return CollectionsUtil.ListOf(element);
    }
}
