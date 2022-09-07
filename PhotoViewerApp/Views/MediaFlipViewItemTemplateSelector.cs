using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.ViewModels;
using System;

namespace PhotoViewerApp.Views;

public class MediaFlipViewItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BitmapFileInfoTemplate { get; set; }
    public DataTemplate? VideoFileInfoTemplate { get; set; }
    public DataTemplate? VectorGraphicFileInfoTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        base.SelectTemplateCore(item);
        return item switch
        {
            BitmapFlipViewItemModel => BitmapFileInfoTemplate!,
            VideoFlipViewItemModel => VideoFileInfoTemplate!,
            VectorGraphicFlipViewItemModel => VectorGraphicFileInfoTemplate!,
            _ => throw new Exception($"Unsupported item type {item.GetType()}")
        };
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        base.SelectTemplateCore(item, container);
        return SelectTemplateCore(item);
    }

}
