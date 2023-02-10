using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhotoViewer.App.Utils;

public interface IMVVMControl<T>
{
    event RoutedEventHandler Loaded;

    event RoutedEventHandler Unloaded;

    event TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> DataContextChanged;

    bool IsLoaded { get; }

    object? DataContext { get; set; }

    void LoadComponent();

    void InitializeBindings();

    void UpdateBindings();

    void StopBindings();

    void ConnectToViewModel(T viewModel);

    void DisconnectFromViewModel(T viewModel);

}
