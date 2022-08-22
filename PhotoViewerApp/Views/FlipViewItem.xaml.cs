using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;
public sealed partial class MediaFlipViewItem : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty<MediaFlipViewItem>.Register(nameof(ViewModel), typeof(MediaFlipViewItemModel), null, (obj, args) => obj.OnViewModelChanged());

    public MediaFlipViewItemModel? ViewModel { get => (MediaFlipViewItemModel?)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }

    public MediaFlipViewItem()
    {
        this.InitializeComponent();
    }

    private void OnViewModelChanged()
    {
        this.Bindings.Update();
    }
}
