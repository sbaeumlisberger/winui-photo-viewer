using Microsoft.UI.Xaml;
using PhotoViewerCore.Utils;
using System;

namespace PhotoViewerApp.Utils;

public class DependencyPropertyHelper<TDependencyObject> where TDependencyObject : DependencyObject
{
    public static DependencyProperty Register(string propertyName, Type propertyType, object? defaultValue = null)
    {
        return DependencyProperty.Register(propertyName, typeof(TDependencyObject), propertyType, new PropertyMetadata(defaultValue));
    }

    public static DependencyProperty Register(string propertyName, Type propertyType, object? defaultValue, Action<TDependencyObject, DependencyPropertyChangedEventArgs> propertyChangedCallback)
    {
        var metadata = new PropertyMetadata(defaultValue, (obj, args) => propertyChangedCallback((TDependencyObject)obj, args));
        return DependencyProperty.Register(propertyName, typeof(TDependencyObject), propertyType, metadata);
    }
}

public class DependencyPropertyHelper
{
    public static DependencyProperty RegisterAttached<TProperty>(Type ownerType, string propertyName, TProperty defaultValue)
    {
        return DependencyProperty.RegisterAttached(propertyName.StripEnd("Property"), ownerType, typeof(TProperty), new PropertyMetadata(defaultValue));
    }

    public static DependencyProperty RegisterAttached<TProperty>(Type ownerType, string propertyName, TProperty defaultValue, Action<DependencyObject, DependencyPropertyChangedEventArgs> propertyChangedCallback)
    {
        var metadata = new PropertyMetadata(defaultValue, (obj, args) => propertyChangedCallback(obj, args));
        return DependencyProperty.RegisterAttached(propertyName.StripEnd("Property"), ownerType, typeof(TProperty), metadata);
    }
}
