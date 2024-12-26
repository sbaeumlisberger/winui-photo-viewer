using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;

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
        private readonly ScrollViewer scrollViewer;

        private readonly PointerEventHandler pointerWheelChangedEventHandler;

        private uint pointerId;

        private Point lastPointerPosition;

        private bool isViewChangeInProgress = false;

        private DateTimeOffset lastChangeViewCallTimestamp = DateTimeOffset.MinValue;

        public AdvancedScrollViewerZoomBehaviour(ScrollViewer scrollViewer)
        {
            this.scrollViewer = scrollViewer;

            pointerWheelChangedEventHandler = new PointerEventHandler(ScrollViewer_PointerWheelChanged);

            scrollViewer.ZoomMode = ZoomMode.Disabled;
            scrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
            scrollViewer.VerticalScrollMode = ScrollMode.Enabled;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.AddHandler(UIElement.PointerWheelChangedEvent, pointerWheelChangedEventHandler, true);
            scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            var content = (UIElement)scrollViewer.Content;
            content.PointerWheelChanged += ScrollViewer_PointerWheelChanged;
            content.PointerPressed += Content_PointerPressed;
            content.PointerMoved += Content_PointerMoved;

            scrollViewer.Unloaded += ScrollViewer_Unloaded;
        }

        private void ScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            scrollViewer.Unloaded -= ScrollViewer_Unloaded;
            scrollViewer.RemoveHandler(UIElement.PointerWheelChangedEvent, pointerWheelChangedEventHandler);
            scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            var content = (UIElement)scrollViewer.Content;
            content.PointerWheelChanged -= ScrollViewer_PointerWheelChanged;
            content.PointerPressed -= Content_PointerPressed;
            content.PointerMoved -= Content_PointerMoved;
        }

        private void ScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            // sometimes the ViewChanged event is not fired, so the flag has to be reset manually after a certain time  
            if (DateTimeOffset.Now > lastChangeViewCallTimestamp + TimeSpan.FromMilliseconds(500))
            {
                isViewChangeInProgress = false;
            }

            if (isViewChangeInProgress)
            {
                return;
            }

            var point = e.GetCurrentPoint((UIElement)sender);

            int delta = point.Properties.MouseWheelDelta;

            double x = point.Position.X;
            double y = point.Position.Y;

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

            isViewChangeInProgress = scrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor, true);
            lastChangeViewCallTimestamp = DateTimeOffset.Now;
        }

        private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            isViewChangeInProgress = false;
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
