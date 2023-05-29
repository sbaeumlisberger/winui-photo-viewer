using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;
using Windows.System;

namespace PhotoViewer.App.Views;

internal static class ScrollViewerExtension
{
    public static void EnableAdvancedZoomBehaviour(this ScrollViewer scrollViewer)
    {
        new AdvancedScrollViewerZoomBehaviour(scrollViewer);
    }

    public static void Zoom(this ScrollViewer scrollViewer, float zoomFactor)
    {
        double zoomDelta = zoomFactor - scrollViewer.ZoomFactor;
        double horizontalOffset = scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth / 2 * zoomDelta;
        double verticalOffset = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight / 2 * zoomDelta;
        scrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor);
    }

    private class AdvancedScrollViewerZoomBehaviour
    {

        private ScrollViewer scrollViewer;

        private uint pointerId;

        private Point lastPointerPosition;

        public AdvancedScrollViewerZoomBehaviour(ScrollViewer scrollViewer)
        {
            this.scrollViewer = scrollViewer;
            scrollViewer.ZoomMode = ZoomMode.Enabled;
            scrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
            scrollViewer.VerticalScrollMode = ScrollMode.Enabled;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.PointerWheelChanged += ScrollViewer_PointerWheelChanged;

            var content = (UIElement)scrollViewer.Content;
            content.PointerWheelChanged += ScrollViewer_PointerWheelChanged;
            content.PointerPressed += Content_PointerPressed;
            content.PointerMoved += Content_PointerMoved;

            scrollViewer.Unloaded += ScrollViewer_Unloaded;
        }

        private void ScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            scrollViewer.Unloaded -= ScrollViewer_Unloaded;
            scrollViewer.PointerWheelChanged -= ScrollViewer_PointerWheelChanged;
            var content = (UIElement)scrollViewer.Content;
            content.PointerWheelChanged -= ScrollViewer_PointerWheelChanged;
            content.PointerPressed -= Content_PointerPressed;
            content.PointerMoved -= Content_PointerMoved;
        }

        private void ScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                return; // skip because of default zoom behavior
            }

            e.Handled = true;

            var content = (UIElement)scrollViewer.Content;

            int delta = e.GetCurrentPoint(content).Properties.MouseWheelDelta;

            double x = e.GetCurrentPoint(content).Position.X;
            double y = e.GetCurrentPoint(content).Position.Y;

            float zoomFactor = 0;
            if (delta < 0)
            {
                zoomFactor = Math.Max(scrollViewer.ZoomFactor * 0.8f, scrollViewer.MinZoomFactor);
            }
            if (delta > 0)
            {
                zoomFactor = Math.Min(scrollViewer.ZoomFactor * 1.2f, scrollViewer.MaxZoomFactor);
            }

            float zoomDelta = zoomFactor - scrollViewer.ZoomFactor;
            double horizontalOffset = scrollViewer.HorizontalOffset + x * zoomDelta;
            double verticalOffset = scrollViewer.VerticalOffset + y * zoomDelta;

            scrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor);
        }

        private void Content_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                pointerId = e.Pointer.PointerId;
                lastPointerPosition = e.GetCurrentPoint((UIElement)sender).Position;
            }
        }

        private void Content_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact && e.Pointer.PointerId == pointerId)
            {
                e.Handled = true;
                Point currentPointerPosition = e.GetCurrentPoint((UIElement)sender).Position;
                double offsetX = scrollViewer.HorizontalOffset - currentPointerPosition.X + lastPointerPosition.X;
                double offsetY = scrollViewer.VerticalOffset - currentPointerPosition.Y + lastPointerPosition.Y;
                scrollViewer.ChangeView(offsetX, offsetY, null, true);
            }
        }
    }

}
