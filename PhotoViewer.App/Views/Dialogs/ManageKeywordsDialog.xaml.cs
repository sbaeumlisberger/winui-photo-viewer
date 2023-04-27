using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewer.App.Views.Dialogs;

[ViewRegistration(typeof(ManageKeywordsDialogModel))]
public sealed partial class ManageKeywordsDialog : ContentDialog, IMVVMControl<ManageKeywordsDialogModel>
{

    private ManageKeywordsDialogModel ViewModel => (ManageKeywordsDialogModel)DataContext;

    public ManageKeywordsDialog()
    {
        this.InitializeComponentMVVM();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        string keyword = (string)((FrameworkElement)sender).DataContext;
        ViewModel.RemoveCommand.ExecuteAsync(keyword);
    }
}
