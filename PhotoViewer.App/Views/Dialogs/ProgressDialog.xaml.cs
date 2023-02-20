using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;
using System;
using PhotoViewer.App.Resources;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using PhotoViewer.Core.Utils;
using PhotoVieweApp.Utils;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(ProgressDialogModel))]
public sealed partial class ProgressDialog : ContentDialog, IMVVMControl<ProgressDialogModel>
{

    private ProgressDialogModel ViewModel => (ProgressDialogModel)DataContext;

    public ProgressDialog()
    {
        this.InitializeComponentMVVM();
    }

    private string? CanCancelToSecondaryButtonText(bool canCancel) => canCancel ? Strings.ProgressDialog_CancelButton : null;

}
