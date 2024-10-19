using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhotoViewer.App.Utils;
using System;
using Windows.UI;

namespace PhotoViewer.App.Controls;

public class RatingChangedEventArgs
{

    public readonly int OldRating;
    public readonly int NewRating;

    public RatingChangedEventArgs(int oldRating, int newRating)
    {
        OldRating = oldRating;
        NewRating = newRating;
    }

}

public sealed partial class RatingControl : UserControl
{
    public static DependencyProperty RatingProperty { get; } = DependencyPropertyHelper<RatingControl>.Register(nameof(Rating), 0, (s, e) => { s.OnRatingChanged(e); });

    public int Rating { get => (int)GetValue(RatingProperty); set => SetValue(RatingProperty, value); }

    public event EventHandler<RatingChangedEventArgs>? RatingChanged;

    private Button? hoverdButton;

    public RatingControl()
    {
        InitializeComponent();
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private void OnRatingChanged(DependencyPropertyChangedEventArgs e)
    {
        if (Rating < 0 || Rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(Rating), "Rating must be between 0 and 5.");
        }
        SetButtons(Rating);
        RatingChanged?.Invoke(this, new RatingChangedEventArgs((int)e.OldValue, Rating));
    }

    private void SetButtons(int rating)
    {
        for (int index = 0; index < panel.Children.Count; index++)
        {
            Button button = (Button)panel.Children[index];
            SymbolIcon icon = (SymbolIcon)button.Content;
            icon.Foreground = (rating > index) ? new SolidColorBrush(Color.FromArgb(255, 255, 200, 0)) : new SolidColorBrush(Colors.Gray);
            if (button != hoverdButton)
            {
                button.RenderTransform = null;
            }
        }
    }

    private void ButtonClicked(object sender, RoutedEventArgs e)
    {
        hoverdButton = null;
        int rating = int.Parse(((Button)sender).CommandParameter.ToString()!);
        Rating = (rating == Rating) ? 0 : rating;
    }

    private void ButtonHovered(object sender, PointerRoutedEventArgs e)
    {
        hoverdButton = (Button)sender;
        int rating = int.Parse(hoverdButton.CommandParameter.ToString()!);
        rating = (rating == Rating) ? 0 : rating;
        SetButtons(rating);
        hoverdButton.RenderTransform = new ScaleTransform
        {
            CenterX = hoverdButton.ActualWidth / 2,
            CenterY = hoverdButton.ActualHeight / 2,
            ScaleX = 1.3,
            ScaleY = 1.3,
        };
    }

    private void PanelExited(object sender, PointerRoutedEventArgs e)
    {
        hoverdButton = null;
        SetButtons(Rating);
    }

}