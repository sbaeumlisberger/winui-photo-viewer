﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using System;
using System.Collections.Generic;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(FlipViewPageModel))]
public sealed partial class FlipViewPage : Page, IMVVMControl<FlipViewPageModel>
{
    private readonly PrintService printService = new PrintService(App.Current.Window);

    private PrintRegistration? printRegistration;

    public FlipViewPage()
    {
        DataContext = App.Current.ViewModelFactory.CreateFlipViewPageModel();
        this.InitializeComponentMVVM();
    }

    partial void DisconnectFromViewModel(FlipViewPageModel viewModel)
    {
        viewModel.Cleanup();
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        if (printRegistration != null)
        {
            printService.Unregister(printRegistration);
        }

        ViewModel!.OnNavigatedFrom();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel!.OnNavigatedTo(e.Parameter, e.NavigationMode != NavigationMode.New);

        printRegistration = printService.RegisterForPrinting(() =>
        {
            if (ViewModel!.FlipViewModel.SelectedItem is { } selectedMediaFile)
            {
                return new PhotoPrintJob(new[] { selectedMediaFile });
            }
            return new PhotoPrintJob(new IMediaFileInfo[0]);
        });
    }
}
