using Essentials.NET;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;

namespace PhotoViewer.App.Controls;

public sealed partial class SelectionRect : UserControl
{
    public class BoundsChangingEventArgs
    {
        public required Rect NewBounds { get; set; }
    }

    public event TypedEventHandler<SelectionRect, EventArgs>? InteractionStarted;
    public event TypedEventHandler<SelectionRect, EventArgs>? InteractionEnded;
    public event TypedEventHandler<SelectionRect, BoundsChangingEventArgs>? BoundsChanging;
    public event TypedEventHandler<SelectionRect, EventArgs>? BoundsChanged;

    public Size AspectRadio { get => aspectRadio; set { aspectRadio = value; OnAspectRadioChanged(); } }
    private Size aspectRadio = Size.Empty;

    public float UIScaleFactor { get => uiScaleFactor; set { uiScaleFactor = value; OnUIScaleFactorChanged(); } }
    private float uiScaleFactor = 1f;

    private double CornerSize { get; } = 8;
    private double StrokeThickness { get; } = 1;

    private double CornerOffset => -CornerSize / 2;

    private readonly PointerEventHandler pointerReleasedEventHandler;
    private readonly PointerEventHandler pointerExitedEventHandler;

    private Canvas? canvas;
    private UIElement? root;

    private FrameworkElement? activeElement;
    private Point startPointerPosition;
    private Rect startBounds;

    public SelectionRect()
    {
        pointerReleasedEventHandler = new PointerEventHandler(Root_PointerReleased);
        pointerExitedEventHandler = new PointerEventHandler(Root_PointerExited);
        Loaded += SelectionRect_Loaded;
        Unloaded += SelectionRect_Unloaded;
        InitializeComponent();
        OnUIScaleFactorChanged();
    }

    public void HandOverPointerPressedEvent(PointerRoutedEventArgs args)
    {
        var position = args.GetCurrentPoint(canvas).Position;
        SetBounds(new Rect(position.X, position.Y, 0, 0));
        OnPointerPressed(cornerRightBottom, args);
    }

    private void SelectionRect_Loaded(object sender, RoutedEventArgs e)
    {
        canvas = (Canvas)Parent;
        canvas.PointerMoved += Canvas_PointerMoved;

        root = XamlRoot.Content;
        root.AddHandler(PointerReleasedEvent, pointerReleasedEventHandler, true);
        root.AddHandler(PointerExitedEvent, pointerExitedEventHandler, true);

        OnAspectRadioChanged();
        OnUIScaleFactorChanged();
    }

    private void SelectionRect_Unloaded(object sender, RoutedEventArgs e)
    {
        if (canvas != null)
        {
            canvas.PointerMoved -= Canvas_PointerMoved;
            canvas = null;
        }

        if (root != null)
        {
            root.RemoveHandler(PointerReleasedEvent, pointerReleasedEventHandler);
            root.RemoveHandler(PointerExitedEvent, pointerExitedEventHandler);
            root = null;
        }
    }

    private void OnAspectRadioChanged()
    {
        if (canvas is null || double.IsNaN(Width) || double.IsNaN(Height))
        {
            return;
        }

        if (TryGetAspectRadioAsDouble() is double aspectRadio)
        {
            if (canvas.ActualHeight < canvas.ActualWidth)
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
        if (UIScaleFactor != 1)
        {
            var cornerTransform = new ScaleTransform()
            {
                ScaleX = UIScaleFactor,
                ScaleY = UIScaleFactor
            };
            cornerLeftTop.RenderTransform = cornerTransform;
            cornerRightTop.RenderTransform = cornerTransform;
            cornerLeftBottom.RenderTransform = cornerTransform;
            cornerRightBottom.RenderTransform = cornerTransform;

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
            cornerLeftTop.RenderTransform = null;
            cornerRightTop.RenderTransform = null;
            cornerLeftBottom.RenderTransform = null;
            cornerRightBottom.RenderTransform = null;
            borderLeft.RenderTransform = null;
            borderRight.RenderTransform = null;
            borderTop.RenderTransform = null;
            borderBottom.RenderTransform = null;
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (activeElement is null)
        {
            ProtectedCursor = InputSystemCursor.Create(GetCursorShapeForElement((FrameworkElement)sender));
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        args.Handled = true;
        activeElement = (FrameworkElement)sender;
        ProtectedCursor = InputSystemCursor.Create(GetCursorShapeForElement(activeElement));
        startPointerPosition = args.GetCurrentPoint(canvas).Position;
        startBounds = GetBounds();
        XamlRoot.Content.PointerReleased += OnPointerReleased;
        InteractionStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndInteraction();
    }

    private void Root_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndInteraction();
    }

    private void Root_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        EndInteraction();
    }

    private void EndInteraction()
    {
        if (activeElement != null)
        {
            activeElement = null;
            startPointerPosition = default;
            startBounds = default;
            XamlRoot.Content.PointerReleased -= OnPointerReleased;
            InteractionEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (activeElement is null)
        {
            return;
        }

        args.Handled = true;

        Point currentPointerPosition = args.GetCurrentPoint(canvas).Position;
        currentPointerPosition.X = Math.Max(0, currentPointerPosition.X);
        currentPointerPosition.X = Math.Min(canvas!.ActualWidth, currentPointerPosition.X);
        currentPointerPosition.Y = Math.Max(0, currentPointerPosition.Y);
        currentPointerPosition.Y = Math.Min(canvas.ActualHeight, currentPointerPosition.Y);

        if (activeElement.Name == rect.Name)
        {
            double newRectX = startBounds.X + currentPointerPosition.X - startPointerPosition.X;
            newRectX = Math.Max(newRectX, 0);
            newRectX = Math.Min(newRectX, canvas.ActualWidth - startBounds.Width);
            double newRectY = startBounds.Y + currentPointerPosition.Y - startPointerPosition.Y;
            newRectY = Math.Max(newRectY, 0);
            newRectY = Math.Min(newRectY, canvas.ActualHeight - startBounds.Height);
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
                double maxHeight = aspectRadio is null ? canvas.ActualHeight : canvas.ActualWidth / (double)aspectRadio;
                double height = Math.Min(MathUtils.Diff(startBounds.Bottom, currentPointerPosition.Y), maxHeight);
                double width = aspectRadio is null ? startBounds.Width : height * (double)aspectRadio;
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
                double maxWidth = aspectRadio is null ? canvas.ActualWidth : canvas.ActualHeight * (double)aspectRadio;
                double width = Math.Min(MathUtils.Diff(startBounds.Left, currentPointerPosition.X), maxWidth);
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
                double maxHeight = aspectRadio is null ? canvas.ActualHeight : canvas.ActualWidth / (double)aspectRadio;
                double height = Math.Min(MathUtils.Diff(startBounds.Top, currentPointerPosition.Y), maxHeight);
                double width = aspectRadio is null ? startBounds.Width : height * (double)aspectRadio;
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
                double maxWidth = aspectRadio is null ? canvas.ActualWidth : canvas.ActualHeight * (double)aspectRadio;
                double width = Math.Min(MathUtils.Diff(startBounds.Right, currentPointerPosition.X), maxWidth);
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
        return new Rect(x, y, Width, Height);
    }

    public void SetBounds(Rect bounds)
    {
        var boundsChanngingEventArgs = new BoundsChangingEventArgs() { NewBounds = bounds };
        BoundsChanging?.Invoke(this, boundsChanngingEventArgs);
        bounds = boundsChanngingEventArgs.NewBounds;
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

    private InputSystemCursorShape GetCursorShapeForElement(FrameworkElement element)
    {
        return element.Name switch
        {
            nameof(borderLeft) => InputSystemCursorShape.SizeWestEast,
            nameof(borderTop) => InputSystemCursorShape.SizeNorthSouth,
            nameof(borderRight) => InputSystemCursorShape.SizeWestEast,
            nameof(borderBottom) => InputSystemCursorShape.SizeNorthSouth,
            nameof(cornerLeftTop) => InputSystemCursorShape.SizeNorthwestSoutheast,
            nameof(cornerRightTop) => InputSystemCursorShape.SizeNortheastSouthwest,
            nameof(cornerLeftBottom) => InputSystemCursorShape.SizeNortheastSouthwest,
            nameof(cornerRightBottom) => InputSystemCursorShape.SizeNorthwestSoutheast,
            nameof(rect) => InputSystemCursorShape.SizeAll,
            _ => throw new UnreachableException(),
        };
    }

    private double Add(double a, double b)
    {
        return a + b;
    }

    private double Substract(double a, double b)
    {
        return a - b;
    }
}
