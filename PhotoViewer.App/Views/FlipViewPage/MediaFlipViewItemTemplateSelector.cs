using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Models;
using System;

namespace PhotoViewer.App.Views;

public partial class MediaFlipViewItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BitmapFileInfoTemplate { get; set; }
    public DataTemplate? VideoFileInfoTemplate { get; set; }
    public DataTemplate? VectorGraphicFileInfoTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        base.SelectTemplateCore(item);
        return item switch
        {
            IBitmapFileInfo => BitmapFileInfoTemplate!,
            IVideoFileInfo => VideoFileInfoTemplate!,
            IVectorGraphicFileInfo => VectorGraphicFileInfoTemplate!,
            _ => throw new Exception($"Unsupported item type {item.GetType()}")
        };
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        base.SelectTemplateCore(item, container);
        return SelectTemplateCore(item);
    }

}
