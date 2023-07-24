using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.App.Controls;

internal class FlipViewPlus : FlipView
{
    public event EventHandler<(DependencyObject Element, object Item)>? PrepareContainer;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        Log.Debug("PrepareContainerForItemOverride " + item);
        base.PrepareContainerForItemOverride(element, item);
        PrepareContainer?.Invoke(this, (element, item));
    }

}
