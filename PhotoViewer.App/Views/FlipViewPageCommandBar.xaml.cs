using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using System.Collections.Generic;

namespace PhotoViewer.App.Views;

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
