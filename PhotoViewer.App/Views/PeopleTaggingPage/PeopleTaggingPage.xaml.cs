using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System.Linq;
using Windows.Graphics.Imaging;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PeopleTaggingPageModel))]
public sealed partial class PeopleTaggingPage : Page, IMVVMControl<PeopleTaggingPageModel>
{
    public PeopleTaggingPage()
    {
        DataContext = App.Current.ViewModelFactory.CreatePeopleTaggingBatchViewPageModel();
        this.InitializeComponentMVVM();
    }

    private void PeopleTaggingPage_Loaded(object sender, RoutedEventArgs e)
    {
        var collectionViewSource = new CollectionViewSource()
        {
            Source = ViewModel!.DetectedFaces,
            IsSourceGrouped = true
        };

        detectedFacesGridView.ItemsSource = collectionViewSource.View;
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
