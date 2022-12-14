using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace PhotoViewerApp.Utils;

internal static class ControlUtil
{

    public static void InitializeMVVM<T>(this Control control, Action initializeComponent, 
        Action<T>? connectToViewModel = null, Action<T>? disconnectFromViewModel = null) where T : ViewModelBase
    {
        if (control.IsLoaded) 
        {
            throw new Exception("Control is already loaded");
        }

        T? viewModel = null;

        void connect(T newViewModel) 
        {
            viewModel = newViewModel;
            connectToViewModel?.Invoke(newViewModel);
            viewModel.OnViewConnected();
        }

        void disconnect(T currentViewModel) 
        {
            disconnectFromViewModel?.Invoke(currentViewModel);
            currentViewModel.OnViewDisconnected();
            viewModel = null;
        }

        control.Loaded += (s, e) =>
        {
            control.DataContextChanged += (s, e) =>
            {
                if (viewModel is T currentViewModel)
                {
                    disconnect(currentViewModel);
                }
                if (control.DataContext is T newViewModel && !ReferenceEquals(newViewModel, viewModel))
                {
                    connect(newViewModel);
                }
            };
            if (control.DataContext is T newViewModel && !ReferenceEquals(newViewModel, viewModel))
            {
                connect(newViewModel);
            }
        };

        control.Unloaded += (s, e) =>
        {
            if (viewModel is T currentViewModel)
            {
                disconnect(currentViewModel);
            }
            control.DataContext = null;
        };

        initializeComponent.Invoke();
    }

    public static void RegisterPropertyChangedCallbackSafely(this Control control, DependencyProperty property, DependencyPropertyChangedCallback propertyChangedCallback)
    {
        long token = control.RegisterPropertyChangedCallback(property, propertyChangedCallback);

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            control.Unloaded -= UserControl_Unloaded;
            control.UnregisterPropertyChangedCallback(property, token);
        }
        control.Unloaded += UserControl_Unloaded;
    }


}
