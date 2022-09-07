using Microsoft.UI.Xaml;
using System;

namespace PhotoViewerApp.Utils;

public class DependencyProperty<TDependencyObject> where TDependencyObject : DependencyObject
{

    public static DependencyProperty Register(string propertyName, Type propertyType, object? defaultValue = null)
    {
        return DependencyProperty.Register(propertyName, typeof(TDependencyObject), propertyType, new PropertyMetadata(defaultValue));
    }

}
