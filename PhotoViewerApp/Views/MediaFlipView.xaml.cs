using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System.ComponentModel;

namespace PhotoViewerApp.Views;
public sealed partial class MediaFlipView : UserControl
{
    private MediaFlipViewModel ViewModel => (MediaFlipViewModel)DataContext;

    public MediaFlipView()
    {
        this.InitializeComponent();
        this.WhenDataContextSet(() =>
        {
            ViewModel.PropertyChanged += FlipViewModel_PropertyChanged;
            UpdateWindowTitle();
        });
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedItemModel))
        {
            UpdateWindowTitle();
        }
    }

    private void UpdateWindowTitle()
    {
        App.Current.Window.Title = ViewModel.SelectedItemModel?.MediaItem.Name ?? "";
    }
}
