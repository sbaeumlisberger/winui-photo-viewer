using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.Storage;
using static System.Collections.Specialized.BitVector32;

namespace PhotoViewerApp.ViewModels;

public partial class FlipViewPageModel : ViewModelBase
{
    public IDetailsBarModel DetailsBarModel { get; }

    public IFlipViewPageCommandBarModel CommandBarModel { get; }

    public IMediaFlipViewModel FlipViewModel { get; }

    private readonly Session session;

    public FlipViewPageModel(
        Session session,
        Func<IMediaFlipViewModel> flipViewModelFactory,
        Func<IDetailsBarModel> detailsBarModelFactory,
        Func<IMediaFlipViewModel, IFlipViewPageCommandBarModel> flipViewPageCommandBarModelFactory)
    {
        this.session = session;

        FlipViewModel = flipViewModelFactory.Invoke();
        DetailsBarModel = detailsBarModelFactory.Invoke();
        CommandBarModel = flipViewPageCommandBarModelFactory.Invoke(FlipViewModel);

        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlipViewModel.SelectedItemModel)) 
        {
            DetailsBarModel.SelectedItemModel = FlipViewModel.SelectedItemModel;
            CommandBarModel.SelectedItemModel = FlipViewModel.SelectedItemModel;
        }
    }

    public void OnNavigatedTo(object navigationParameter) 
    {
        FlipViewModel.SetItems(session.MediaItems, (IMediaItem?)navigationParameter);
    }
}
