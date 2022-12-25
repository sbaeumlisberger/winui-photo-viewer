using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Resources;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using System;

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

    private string ResolvePlaceholder(bool hasDifferentValues, string placeholder) 
    {
        return hasDifferentValues ? Strings.MetadataPanel_DifferentValuesPlaceholder : placeholder;
    }

}
