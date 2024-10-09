using Essentials.NET.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(FlipViewPageModel))]
public sealed partial class FlipViewPage : Page, IMVVMControl<FlipViewPageModel>
{
    private readonly PrintService printService = new PrintService(App.Current.Window);

    private PrintRegistration? printRegistration;

    public FlipViewPage()
    {
        DataContext = App.Current.ViewModelFactory.CreateFlipViewPageModel();
        Loaded += FlipViewPage_Loaded;
        InitializeComponentMVVM();
    }

    private void FlipViewPage_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("Flip view page loaded");

        LoadWithLowPriority(nameof(commandBar), commandBarPlaceholder,
            () => LoadWithLowPriority(nameof(detailsBar), detailsBarPlaceholder,
                () => LoadWithLowPriority(nameof(metadataPanel), metadataPanelPlaceholder,
                    () => LoadWithLowPriority(nameof(editImageOverlay), null))));
    }

    private void LoadWithLowPriority(string controlName, FrameworkElement? placeholder, Action? callback = null)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            Log.Debug("Load " + controlName);
            FindName(controlName);
            if (placeholder is not null)
            {
                placeholder.Visibility = Visibility.Collapsed;
            }
            callback?.Invoke();
        });
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
            return new PhotoPrintJob(Array.Empty<IMediaFileInfo>());
        });
    }
}
