using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Linq;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PeopleTaggingPageModel))]
public sealed partial class PeopleTaggingPage : Page, IMVVMControl<PeopleTaggingPageModel>
{
    public PeopleTaggingPage()
    {
        DataContext = App.Current.ViewModelFactory.CreatePeopleTaggingBatchViewPageModel();
        this.InitializeComponentMVVM();
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel!.SelectedFaces = ((GridView)sender).SelectedItems.Cast<DetectedFace>().ToList();
    }

    private void ToolTip_Opened(object sender, RoutedEventArgs e)
    {
        var tooltip = (ToolTip)sender;
        var canvasImageControl = (CanvasImageControl)tooltip.Content;
        var face = (DetectedFace)tooltip.DataContext;
        canvasImageControl.CanvasImage = face.SourceImage;
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        ViewModel!.NameSelectedCommand.Execute((string)e.ClickedItem);
    }

}
