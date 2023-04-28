using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

internal static class ControlUtil
{
    public static void InitializeComponentMVVM<TViewModel>(this IMVVMControl<TViewModel> control, bool updateBindingsAlways = false) where TViewModel : class, IViewModel
    {
        TViewModel? viewModel = null;

        void connect(TViewModel newViewModel)
        {
            Log.Debug($"Connect {control} to {newViewModel}");
            viewModel = newViewModel;
            control.ConnectToViewModel(newViewModel);
            if(control.IsLoaded || updateBindingsAlways) // TODO ?
            {
                control.UpdateBindings();
            }
        }

        void disconnect(TViewModel currentViewModel)
        {
            Log.Debug($"Disconnect {control} from {currentViewModel}");
            control.DisconnectFromViewModel(viewModel);
            control.StopBindings();
            viewModel = null;
        }

        control.Unloaded += (s, e) =>
        {
            if (viewModel is TViewModel currentViewModel)
            {
                disconnect(currentViewModel);
            }
            control.DataContext = null;
        };

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

        control.InitializeComponent();
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
