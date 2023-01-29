using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils.Logging;
using PhotoViewerCore.Utils;
using System;
using System.Reflection;

namespace PhotoViewer.App.Utils;

internal static class ControlUtil
{
    public static void InitializeMVVM<TViewModel>(
        this IMVVMControl<TViewModel> control,
        Action<TViewModel>? connectToViewModel = null,
        Action<TViewModel>? disconnectFromViewModel = null)
        where TViewModel : ViewModelBase
    {
        if (control.IsLoaded)
        {
            throw new Exception("Control is already loaded");
        }

        TViewModel? viewModel = null;

        void connect(TViewModel newViewModel)
        {
            Log.Debug($"Connect {control} to {newViewModel}");
            viewModel = newViewModel;
            connectToViewModel?.Invoke(newViewModel);
            control.UpdateBindings(); // TODO ?
            viewModel.OnViewConnected();
        }

        void disconnect(TViewModel currentViewModel)
        {
            Log.Debug($"Disconnect {control} from {currentViewModel}");
            disconnectFromViewModel?.Invoke(viewModel);
            control.StopBindings();
            currentViewModel.OnViewDisconnected();
            viewModel = null;
        }

        control.Loaded += (s, e) =>
        {
            control.DataContextChanged += (s, e) =>
            {
                if (e.NewValue != viewModel)
                {
                    if (viewModel is TViewModel currentViewModel)
                    {
                        disconnect(currentViewModel);
                    }
                    if (e.NewValue is TViewModel newViewModel)
                    {
                        connect(newViewModel);
                    }
                }
            };
            if (control.DataContext is TViewModel newViewModel && newViewModel != viewModel)
            {
                connect(newViewModel);
            }
        };

        control.Unloaded += (s, e) =>
        {
            if (viewModel is TViewModel currentViewModel)
            {
                disconnect(currentViewModel);
            }
            control.DataContext = null;
        };

        control.LoadComponent();
    }

    public static long RegisterPropertyChangedCallbackSafely(this Control control, DependencyProperty property, DependencyPropertyChangedCallback propertyChangedCallback)
    {
        long token = control.RegisterPropertyChangedCallback(property, propertyChangedCallback);

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            control.Unloaded -= UserControl_Unloaded;
            control.UnregisterPropertyChangedCallback(property, token);
        }
        control.Unloaded += UserControl_Unloaded;

        return token;
    }

}
