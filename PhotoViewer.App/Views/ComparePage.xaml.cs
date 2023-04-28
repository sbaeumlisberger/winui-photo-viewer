using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(ComparePageModel))]
public sealed partial class ComparePage : Page, IMVVMControl<ComparePageModel>
{
    private ComparePageModel ViewModel => (ComparePageModel)DataContext;

    public ComparePage()
    {
        DataContext = ViewModelFactory.Instance.CreateComparePageModel();
        this.InitializeComponentMVVM();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.Cleanup();
    }
}
