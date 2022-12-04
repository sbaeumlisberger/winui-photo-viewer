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
             connectToViewModel: () =>
             {
                 ViewModel.PeopleSectionModel.OnViewConnected();
                 ViewModel.KeywordsSectionModel.OnViewConnected();
                 Bindings.Initialize();
             },
            disconnectFromViewModel: () =>
            {
                ViewModel.PeopleSectionModel.OnViewDisconnected();
                ViewModel.KeywordsSectionModel.OnViewDisconnected();
                Bindings.StopTracking();
            });
    }

}
