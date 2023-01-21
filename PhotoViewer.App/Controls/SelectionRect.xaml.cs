using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhotoVieweApp.Utils;
using PhotoViewerApp.Utils;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;

namespace PhotoViewerApp.Controls;
public sealed partial class SelectionRect : UserControl
{

    public static readonly DependencyProperty AspectRadioProperty = DependencyPropertyHelper<SelectionRect>.Register(nameof(AspectRadio), typeof(Size), Size.Empty, (s, e) => s.OnAspectRadioChanged());
    public static readonly DependencyProperty UIScaleFactorProperty = DependencyPropertyHelper<SelectionRect>.Register(nameof(UIScaleFactor), typeof(float), 1f, (s, e) => s.OnUIScaleFactorChanged());

    public event TypedEventHandler<SelectionRect, EventArgs>? InteractionStarted;
    public event TypedEventHandler<SelectionRect, EventArgs>? InteractionEnded;
    public event TypedEventHandler<SelectionRect, EventArgs>? BoundsChanged;

    public Size AspectRadio { get => (Size)GetValue(AspectRadioProperty); set => SetValue(AspectRadioProperty, value); }
    public float UIScaleFactor { get => (float)GetValue(UIScaleFactorProperty); set => SetValue(UIScaleFactorProperty, value); }


    // TODO use resources
    private double CornerSize => 8;
    private double StrokeThickness => 1;

    private Canvas Canvas => (Canvas)Parent;

    private FrameworkElement? activeElement;
    private Point startPointerPosition;
    private Rect startBounds;

    public SelectionRect()
    {
        Loaded += SelectionRect_Loaded;
        InitializeComponent();
        OnUIScaleFactorChanged();
    }

    public void HandOverPointerPressedEvent(PointerRoutedEventArgs args)
    {
        OnPointerPressed(cornerRightBottom, args);
    }

    private void SelectionRect_Loaded(object sender, RoutedEventArgs e)
    {
        cornerLeftTop.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
        cornerRightTop.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest));
        cornerRightBottom.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
        cornerLeftBottom.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest));
        borderLeft.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        borderTop.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth));
        borderRight.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        borderBottom.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth));
        rect.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeAll));
    }

    private void OnAspectRadioChanged()
    {
        double? aspectRadio = TryGetAspectRadioAsDouble();
        if (aspectRadio != null)
        {
            if (Canvas.ActualHeight < Canvas.ActualWidth)
            {
                Width = (double)aspectRadio * Height;
            }
            else
            {
                Height = (1 / (double)aspectRadio) * Width;
            }
            BoundsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnUIScaleFactorChanged()
    {
        double cornerOffset = CornerSize / 2 - StrokeThickness / 2;
        var cornerLeftTopTranslateTransform = new TranslateTransform() { X = -cornerOffset, Y = -cornerOffset };
        var cornerRightTopTranslateTransform = new TranslateTransform() { X = cornerOffset, Y = -cornerOffset };
        var cornerRightBottomTranslateTransform = new TranslateTransform() { X = cornerOffset, Y = cornerOffset };
        var cornerLeftBottomTranslateTransform = new TranslateTransform() { X = -cornerOffset, Y = cornerOffset };

        if (UIScaleFactor != 1)
        {
            var cornerScaleTransform = new ScaleTransform()
            {
                ScaleX = UIScaleFactor,
                ScaleY = UIScaleFactor
            };
            cornerLeftTop.RenderTransform = new TransformGroup() { Children = { cornerLeftTopTranslateTransform, cornerScaleTransform } };
            cornerRightTop.RenderTransform = new TransformGroup() { Children = { cornerRightTopTranslateTransform, cornerScaleTransform } };
            cornerRightBottom.RenderTransform = new TransformGroup() { Children = { cornerRightBottomTranslateTransform, cornerScaleTransform } };
            cornerLeftBottom.RenderTransform = new TransformGroup() { Children = { cornerLeftBottomTranslateTransform, cornerScaleTransform } };

            var borderLeftRightTransform = new ScaleTransform()
            {
                ScaleX = UIScaleFactor,
            };
            borderLeft.RenderTransform = borderLeftRightTransform;
            borderRight.RenderTransform = borderLeftRightTransform;

            var borderTopBottomTransform = new ScaleTransform()
            {
                ScaleY = UIScaleFactor,
            };
            borderTop.RenderTransform = borderTopBottomTransform;
            borderBottom.RenderTransform = borderTopBottomTransform;
        }
        else
        {
            cornerLeftTop.RenderTransform = cornerLeftTopTranslateTransform;
            cornerRightTop.RenderTransform = cornerRightTopTranslateTransform;
            cornerRightBottom.RenderTransform = cornerRightBottomTranslateTransform;
            cornerLeftBottom.RenderTransform = cornerLeftBottomTranslateTransform;
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        args.Handled = true;
        activeElement = (FrameworkElement)sender;
        startPointerPosition = args.GetCurrentPoint(Canvas).Position;
        startBounds = GetBounds();
        Canvas.PointerMoved += OnPointerMoved;
        XamlRoot.Content.PointerReleased += OnPointerReleased;
        InteractionStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        activeElement = null;
        startPointerPosition = default;
        startBounds = default;
        Canvas.PointerMoved -= OnPointerMoved;
        XamlRoot.Content.PointerReleased -= OnPointerReleased;
        InteractionEnded?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (activeElement is null)
        {
            Debug.Assert(activeElement is not null, $"{nameof(activeElement)} is null");
            return;
        }

        args.Handled = true;

        Point currentPointerPosition = args.GetCurrentPoint(Canvas).Position;
        currentPointerPosition.X = Math.Max(0, currentPointerPosition.X);
        currentPointerPosition.X = Math.Min(Canvas.ActualWidth, currentPointerPosition.X);
        currentPointerPosition.Y = Math.Max(0, currentPointerPosition.Y);
        currentPointerPosition.Y = Math.Min(Canvas.ActualHeight, currentPointerPosition.Y);

        if (activeElement.Name == rect.Name)
        {
            double newRectX = startBounds.X + currentPointerPosition.X - startPointerPosition.X;
            newRectX = Math.Max(newRectX, 0);
            newRectX = Math.Min(newRectX, Canvas.ActualWidth - startBounds.Width);
            double newRectY = startBounds.Y + currentPointerPosition.Y - startPointerPosition.Y;
            newRectY = Math.Max(newRectY, 0);
            newRectY = Math.Min(newRectY, Canvas.ActualHeight - startBounds.Height);
            SetBounds(new Rect(newRectX, newRectY, startBounds.Width, startBounds.Height));
        }
        else
        {
            double? aspectRadio = TryGetInteractiveAspectRadioAsDouble(args.KeyModifiers);

            if (activeElement.Name == nameof(cornerLeftTop))
            {
                Point anchorPos = new Point(startBounds.Right, startBounds.Bottom);
                Point targetPos = new Point(currentPointerPosition.X, currentPointerPosition.Y);
                SetBounds(CreateRectAdjustedToAspectRadio(anchorPos, targetPos, args.KeyModifiers));
            }
            else if (activeElement.Name == nameof(borderTop))
            {
                double maxHeight = aspectRadio is null ? Canvas.ActualHeight : Canvas.ActualWidth / (double)aspectRadio;
                double height = Math.Min(MathUtil.Diff(startBounds.Bottom, currentPointerPosition.Y), maxHeight);
                double width = aspectRadio is null ? startBounds.Width : height / (double)aspectRadio;
                double y = currentPointerPosition.Y < startBounds.Bottom ? startBounds.Bottom - height : startBounds.Bottom;
                double x = Math.Max((startBounds.X + startBounds.Width / 2) - width / 2, 0);
                SetBounds(new Rect(x, y, width, height));
            }
            else if (activeElement.Name == nameof(cornerRightTop))
            {
                Point anchorPos = new Point(startBounds.Left, startBounds.Bottom);
                Point targetPos = new Point(currentPointerPosition.X, currentPointerPosition.Y);
                SetBounds(CreateRectAdjustedToAspectRadio(anchorPos, targetPos, args.KeyModifiers));
            }
            else if (activeElement.Name == nameof(borderRight))
            {
                double maxWidth = aspectRadio is null ? Canvas.ActualWidth : Canvas.ActualHeight * (double)aspectRadio;
                double width = Math.Min(MathUtil.Diff(startBounds.Left, currentPointerPosition.X), maxWidth);
                double height = aspectRadio is null ? startBounds.Height : width / (double)aspectRadio;
                double x = currentPointerPosition.X < startBounds.Left ? startBounds.Left - width : startBounds.Left;
                double y = Math.Max((startBounds.Y + startBounds.Height / 2) - height / 2, 0);
                SetBounds(new Rect(x, y, width, height));
            }
            else if (activeElement.Name == nameof(cornerRightBottom))
            {
                Point anchorPos = new Point(startBounds.Left, startBounds.Top);
                Point targetPos = new Point(currentPointerPosition.X, currentPointerPosition.Y);
                SetBounds(CreateRectAdjustedToAspectRadio(anchorPos, targetPos, args.KeyModifiers));
            }
            else if (activeElement.Name == nameof(borderBottom))
            {
                double maxHeight = aspectRadio is null ? Canvas.ActualHeight : Canvas.ActualWidth / (double)aspectRadio;
                double height = Math.Min(MathUtil.Diff(startBounds.Top, currentPointerPosition.Y), maxHeight);
                double width = aspectRadio is null ? startBounds.Width : height / (double)aspectRadio;
                double y = currentPointerPosition.Y < startBounds.Top ? startBounds.Top - height : startBounds.Top;
                double x = Math.Max((startBounds.X + startBounds.Width / 2) - width / 2, 0);
                SetBounds(new Rect(x, y, width, height));
            }
            else if (activeElement.Name == nameof(cornerLeftBottom))
            {
                Point anchorPos = new Point(startBounds.Right, startBounds.Top);
                Point targetPos = new Point(currentPointerPosition.X, currentPointerPosition.Y);
                SetBounds(CreateRectAdjustedToAspectRadio(anchorPos, targetPos, args.KeyModifiers));
            }
            else if (activeElement.Name == nameof(borderLeft))
            {
                double maxWidth = aspectRadio is null ? Canvas.ActualWidth : Canvas.ActualHeight * (double)aspectRadio;
                double width = Math.Min(MathUtil.Diff(startBounds.Right, currentPointerPosition.X), maxWidth);
                double height = aspectRadio is null ? startBounds.Height : width / (double)aspectRadio;
                double x = currentPointerPosition.X < startBounds.Right ? startBounds.Right - width : startBounds.Right;
                double y = Math.Max((startBounds.Y + startBounds.Height / 2) - height / 2, 0);
                SetBounds(new Rect(x, y, width, height));
            }
        }
    }

    public Rect GetBounds()
    {
        double x = Canvas.GetLeft(this);
        double y = Canvas.GetTop(this);
        double width = Width;
        double height = Height;
        return new Rect(x, y, width, height);
    }

    private void SetBounds(Rect bounds)
    {
        Canvas.SetLeft(this, bounds.X);
        Canvas.SetTop(this, bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;
        BoundsChanged?.Invoke(this, EventArgs.Empty);
    }

    private double? TryGetInteractiveAspectRadioAsDouble(VirtualKeyModifiers keyModifiers)
    {
        double? aspectRadio = TryGetAspectRadioAsDouble();
        if (AspectRadio.IsEmpty && keyModifiers.HasFlag(VirtualKeyModifiers.Shift))
        {
            aspectRadio = Width / Height;
        }
        return aspectRadio;
    }

    public double? TryGetAspectRadioAsDouble()
    {
        if (AspectRadio.IsEmpty)
        {
            return null;
        }
        return AspectRadio.Width / AspectRadio.Height;
    }

    private Rect CreateRectAdjustedToAspectRadio(Point anchorPos, Point targetPos, VirtualKeyModifiers keyModifiers)
    {
        if (TryGetInteractiveAspectRadioAsDouble(keyModifiers) is double aspectRadio)
        {
            double targetWidth = Math.Abs(anchorPos.X - targetPos.X);
            double targetHeight = Math.Abs(anchorPos.Y - targetPos.Y);
            double targetAspectRadio = (targetWidth / targetHeight);
            if (targetAspectRadio < aspectRadio)
            {
                double sign = targetPos.Y < anchorPos.Y ? 1 : -1;
                double adjustedHeight = targetWidth / aspectRadio;
                targetPos.Y += sign * (targetHeight - adjustedHeight);
            }
            else if (targetAspectRadio > aspectRadio)
            {
                double sign = targetPos.X < anchorPos.X ? 1 : -1;
                double adjustedWidth = targetHeight * aspectRadio;
                targetPos.X += sign * (targetWidth - adjustedWidth);
            }
        }
        return new Rect(anchorPos, targetPos);
    }

}
