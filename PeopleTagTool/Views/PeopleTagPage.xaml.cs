using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PeopleTagTool.Services;
using PeopleTagTool.ViewModels;
using System.Linq;
using Windows.System;

namespace PeopleTagTool.Views;

public sealed partial class PeopleTagPage : UserControl
{
    private PeopleTagPageModel ViewModel { get; } = new PeopleTagPageModel(new DialogService(() => App.MainWindow.AppWindow));

    public PeopleTagPage()
    {
        InitializeComponent();
        Loaded += PeopleTagPage_Loaded;
    }

    private void PeopleTagPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Closed += Window_Closed;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        ViewModel.OnWindowClosed();
    }

    private async void PersonNameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            await ViewModel.TagSelectedFacesAsync(((TextBox)sender).Text.Trim());
        }
    }

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        await ViewModel.TagSelectedFacesAsync((string)e.ClickedItem);
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelecetedFaces = gridView.SelectedItems.Cast<DetectedFaceViewModel>().ToList();
    }

  
}
