using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.Views;
public sealed partial class MetadataPanel : UserControl
{
    public MetadataPanelModel ViewModel => (MetadataPanelModel)DataContext;

    public MetadataPanel()
    {
        this.InitializeMVVM<MetadataPanelModel>(InitializeComponent,
             connectToViewModel: (viewModel) =>
             {
                 ViewModel.PeopleSectionModel.OnViewConnected();
                 ViewModel.KeywordsSectionModel.OnViewConnected();
                 Bindings.Initialize();
             },
            disconnectFromViewModel: (viewModel) =>
            {
                viewModel.PeopleSectionModel.OnViewDisconnected();
                viewModel.KeywordsSectionModel.OnViewDisconnected();
                Bindings.StopTracking();
            });
    }

}
