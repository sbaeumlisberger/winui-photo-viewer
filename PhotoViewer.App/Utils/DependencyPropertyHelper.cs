using Essentials.NET;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.CompilerServices;

namespace PhotoViewer.App.Utils;

public static class DependencyPropertyHelper<TDependencyObject>
{
    public static DependencyProperty Register<TProperty>(string propertyName, TProperty defaultValue)
    {
        return DependencyProperty.Register(propertyName, typeof(TProperty), typeof(TDependencyObject), new PropertyMetadata(defaultValue));
    }

    public static DependencyProperty Register<TProperty>(string propertyName, TProperty defaultValue, Action<TDependencyObject, DependencyPropertyChangedEventArgs> propertyChangedCallback)
    {
        var metadata = new PropertyMetadata(defaultValue, (obj, args) => propertyChangedCallback((TDependencyObject)(object)obj, args));
        return DependencyProperty.Register(propertyName, typeof(TProperty), typeof(TDependencyObject), metadata);
    }

    public static DependencyProperty RegisterAttached<TProperty>(TProperty defaultValue, [CallerMemberName] string propertyName = "")
    {
        return DependencyProperty.RegisterAttached(propertyName.RemoveEnd("Property"), typeof(TProperty), typeof(TDependencyObject), new PropertyMetadata(defaultValue));
    }

    public static DependencyProperty RegisterAttached<TProperty>(TProperty defaultValue, Action<DependencyObject, DependencyPropertyChangedEventArgs> propertyChangedCallback, [CallerMemberName] string propertyName = "")
    {
        var metadata = new PropertyMetadata(defaultValue, (obj, args) => propertyChangedCallback(obj, args));
        return DependencyProperty.RegisterAttached(propertyName.RemoveEnd("Property"), typeof(TProperty), typeof(TDependencyObject), metadata);
    }
}
