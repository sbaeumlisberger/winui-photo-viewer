using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;

namespace PhotoViewer.App.Controls;

internal partial class PercentagePlacement : ContentPresenter
{
    public static readonly DependencyProperty PercentageXProperty = DependencyPropertyHelper<PercentagePlacement>.Register(nameof(PercentageX), 0d, (obj, args) => obj.UpdateBounds());
    public static readonly DependencyProperty PercentageYProperty = DependencyPropertyHelper<PercentagePlacement>.Register(nameof(PercentageY), 0d, (obj, args) => obj.UpdateBounds());
    public static readonly DependencyProperty PercentageWidthProperty = DependencyPropertyHelper<PercentagePlacement>.Register(nameof(PercentageWidth), double.NaN, (obj, args) => obj.UpdateBounds());
    public static readonly DependencyProperty PercentageHeightProperty = DependencyPropertyHelper<PercentagePlacement>.Register(nameof(PercentageHeight), double.NaN, (obj, args) => obj.UpdateBounds());

    public static readonly DependencyProperty CenterXProperty = DependencyPropertyHelper<PercentagePlacement>.Register(nameof(CenterX), false, (obj, args) => obj.UpdateBounds());

    public static readonly DependencyProperty FitParentProperty = DependencyPropertyHelper<PercentagePlacement>.Register(nameof(FitParent), false, (obj, args) => obj.UpdateBounds());

    public double PercentageX { get => (double)GetValue(PercentageXProperty); set => SetValue(PercentageXProperty, value); }
    public double PercentageY { get => (double)GetValue(PercentageYProperty); set => SetValue(PercentageYProperty, value); }
    public double PercentageWidth { get => (double)GetValue(PercentageWidthProperty); set => SetValue(PercentageWidthProperty, value); }
    public double PercentageHeight { get => (double)GetValue(PercentageHeightProperty); set => SetValue(PercentageHeightProperty, value); }

    public bool CenterX { get => (bool)GetValue(CenterXProperty); set => SetValue(CenterXProperty, value); }

    public bool FitParent { get => (bool)GetValue(FitParentProperty); set => SetValue(FitParentProperty, value); }


    private Canvas? canvas;

    public PercentagePlacement()
    {
        Loaded += PercentagePlacement_Loaded;
        Unloaded += PercentagePlacement_Unloaded;
        SizeChanged += PercentagePlacement_SizeChanged;
    }

    private void PercentagePlacement_Loaded(object sender, RoutedEventArgs e)
    {
        canvas = (Canvas)Parent;
        canvas.SizeChanged += Canvas_SizeChanged;
        UpdateBounds();
    }

    private void PercentagePlacement_Unloaded(object sender, RoutedEventArgs e)
    {
        if (canvas != null)
        {
            canvas.SizeChanged -= Canvas_SizeChanged;
            canvas = null;
        }
    }

    private void PercentagePlacement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (CenterX || FitParent)
        {
            UpdateBounds();
        }
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBounds();
    }

    private void UpdateBounds()
    {
        if (canvas != null)
        {
            if (CenterX)
            {
                Canvas.SetLeft(this, FitX(PercentageX * canvas.ActualWidth - this.ActualWidth / 2));
            }
            else
            {
                Canvas.SetLeft(this, FitX(PercentageX * canvas.ActualWidth));
            }

            Canvas.SetTop(this, FitY(PercentageY * canvas.ActualHeight));

            if (PercentageWidth != double.NaN)
            {
                this.Width = PercentageWidth * canvas.ActualWidth;
            }

            if (PercentageHeight != double.NaN)
            {
                this.Height = PercentageHeight * canvas.ActualHeight;
            }
        }
    }

    private double FitX(double x)
    {
        if (!FitParent)
        {
            return x;
        }

        if (x < 0)
        {
            return 0;
        }

        if (x > canvas!.ActualWidth - this.ActualWidth)
        {
            return canvas.ActualWidth - this.ActualWidth;
        }

        return x;
    }

    private double FitY(double y)
    {
        if (!FitParent)
        {
            return y;
        }

        if (y < 0)
        {
            return 0;
        }

        if (y > canvas!.ActualHeight - this.ActualHeight)
        {
            return canvas.ActualHeight - this.ActualHeight;
        }

        return y;
    }
}
