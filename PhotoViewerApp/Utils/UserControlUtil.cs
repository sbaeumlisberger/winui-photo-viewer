using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace PhotoViewerApp.Utils;

internal static class UserControlUtil
{

    public static void InitializeMVVM<T>(this UserControl userControl, Action initializeComponent, 
        Action<T>? connectToViewModel = null, Action<T>? disconnectFromViewModel = null) where T : ViewModelBase
    {
        if (userControl.IsLoaded) 
        {
            throw new Exception("UserControl is already loaded");
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

        userControl.Loaded += (s, e) =>
        {
            userControl.DataContextChanged += (s, e) =>
            {
                if (viewModel is T currentViewModel)
                {
                    disconnect(currentViewModel);
                }
                if (userControl.DataContext is T newViewModel && !ReferenceEquals(newViewModel, viewModel))
                {
                    connect(newViewModel);
                }
            };
            if (userControl.DataContext is T newViewModel && !ReferenceEquals(newViewModel, viewModel))
            {
                connect(newViewModel);
            }
        };

        userControl.Unloaded += (s, e) =>
        {
            if (viewModel is T currentViewModel)
            {
                disconnect(currentViewModel);
            }
            userControl.DataContext = null;
        };

        initializeComponent.Invoke();
    }

    public static void RegisterPropertyChangedCallbackSafely(this UserControl userControl, DependencyProperty property, DependencyPropertyChangedCallback propertyChangedCallback)
    {
        long token = userControl.RegisterPropertyChangedCallback(property, propertyChangedCallback);

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            userControl.Unloaded -= UserControl_Unloaded;
            userControl.UnregisterPropertyChangedCallback(property, token);
        }
        userControl.Unloaded += UserControl_Unloaded;
    }


}
