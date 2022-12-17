using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.Views;
public sealed partial class MetadataPanel : UserControl, IMVVMControl<MetadataPanelModel>
{
    public MetadataPanelModel ViewModel => (MetadataPanelModel)DataContext;

    public MetadataPanel()
    {
        this.InitializeMVVM(
            connectToViewModel: (viewModel) =>
            {
                viewModel.PeopleSectionModel.OnViewConnected();
                viewModel.KeywordsSectionModel.OnViewConnected();
            },
            disconnectFromViewModel: (viewModel) =>
            {
                viewModel.PeopleSectionModel.OnViewDisconnected();
                viewModel.KeywordsSectionModel.OnViewDisconnected();
            });
    }

}
