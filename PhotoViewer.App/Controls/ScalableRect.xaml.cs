using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;

namespace PhotoViewer.App.Controls;

public sealed partial class ScalableRect : UserControl
{
    public static readonly DependencyProperty ScaleFactorProperty = DependencyPropertyHelper<ScalableRect>.Register(nameof(ScaleFactor), 1f, (obj, e) => obj.OnScaleFactorChanged());

    public float ScaleFactor { get => (float)GetValue(ScaleFactorProperty); set => SetValue(ScaleFactorProperty, value); }

    public ScalableRect()
    {
        this.InitializeComponent();
    }
    private void OnScaleFactorChanged()
    {
        ScaleXTransform.ScaleX = ScaleFactor;
        ScaleYTransform.ScaleY = ScaleFactor;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        lineLeft.Height = ActualHeight;

        lineRight.Height = ActualHeight;
        Canvas.SetLeft(lineRight, ActualWidth - lineRight.Width);

        lineTop.Width = ActualWidth;

        lineBottom.Width = ActualWidth;
        Canvas.SetTop(lineBottom, ActualHeight - lineBottom.Height);
    }
}
