using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewer.App.Views.Dialogs;

public sealed partial class UnhandledExceptionDialog : ContentDialog
{
    public bool IsSendCrashReportChecked => sendCrashReportCheckBox.IsChecked ?? false;

    public UnhandledExceptionDialog(Window window, string exceptionMessage)
    {
        XamlRoot = window.Content.XamlRoot;
        RequestedTheme = ((FrameworkElement)window.Content).RequestedTheme;
        Title = "Unhandled Exception";
        this.InitializeComponent();
        messageTextBlock.Text = "An unhandled exception occurred: " + exceptionMessage;
    }
}
