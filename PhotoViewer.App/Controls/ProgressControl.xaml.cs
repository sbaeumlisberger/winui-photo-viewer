using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewer.App.Controls;

public sealed partial class ProgressControl : UserControl
{
    public static DependencyProperty ProgressProperty = DependencyPropertyHelper<ProgressControl>.Register(nameof(Progress), typeof(Progress));

    public Progress Progress { get => (Progress)GetValue(ProgressProperty); set=> SetValue(ProgressProperty, value); }

    public ProgressControl()
    {
        this.InitializeComponent();
    }

    private double ToPercent(double progress) => progress * 100;

    private string FormatTimeSpan(TimeSpan? timeSpan) => timeSpan != null ? TimeSpanFormatter.Format(timeSpan.Value) : "";

    private string FormatAsPercent(double progress) => Math.Round(progress * 100) + "%";
}
