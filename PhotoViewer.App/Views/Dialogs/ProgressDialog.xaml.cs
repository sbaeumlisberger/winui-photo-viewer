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
        this.InitializeMVVM();
    }

    private string? CanCancelToSecondaryButtonText(bool canCancel) => canCancel ? Strings.ProgressDialog_CancelButton : null;

    private double ToPercent(double progress) => progress * 100;

    private string FormatTimeSpan(TimeSpan? timeSpan) => timeSpan != null ? TimeSpanFormatter.Format(timeSpan.Value) : "";

    private string FormatAsPercent(double progress) => Math.Round(progress * 100) + "%";

}
