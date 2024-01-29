using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using System;
using System.Collections.Generic;

namespace PhotoViewer.App.Views;

public sealed partial class FlipViewPageCommandBar : CommandBar, IMVVMControl<FlipViewPageCommandBarModel>
{
    public FlipViewPageCommandBar()
    {
        this.InitializeComponentMVVM();
    }

    private List<IMediaFileInfo> ListOf(IMediaFileInfo? element)
    {
        return element != null 
            ? new List<IMediaFileInfo>(1) { element } 
            : new List<IMediaFileInfo>();
    }

}
