using Essentials.NET;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using System;

namespace PhotoViewer.App.Controls;

public sealed partial class ProgressControl : UserControl
{
    public static readonly DependencyProperty ProgressProperty = DependencyPropertyHelper<ProgressControl>.Register(nameof(Progress), typeof(Progress));

    public Progress Progress { get => (Progress)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    public ProgressControl()
    {
        this.InitializeComponent();
    }

    private double ToPercent(double progress) => progress * 100;

    private string FormatTimeSpan(TimeSpan? timeSpan) => timeSpan != null ? timeSpan.Value.ToReadableString() : "";

    private string FormatAsPercent(double progress) => Math.Round(progress * 100) + "%";
}
