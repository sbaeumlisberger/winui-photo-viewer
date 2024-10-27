using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhotoViewer.App.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;

namespace PhotoViewer.App.Controls;

public enum SuggestionListDirection
{
    Auto,
    Bottom,
    Top
}

public sealed partial class AutoSuggestionBox : UserControl
{
    public event TypedEventHandler<AutoSuggestionBox, EventArgs>? SuggestionsRequested;

    public event TypedEventHandler<AutoSuggestionBox, EventArgs>? Submitted;

    public static readonly DependencyProperty TextProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register(nameof(Text), "");

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register(nameof(PlaceholderText), "");

    public static readonly DependencyProperty ItemsSourceProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register<IEnumerable?>(nameof(ItemsSource), null, (obj, args) => obj.OnItemsSourceChanged());

    public static readonly DependencyProperty ItemTemplateProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register<DataTemplate?>(nameof(ItemTemplate), null);

    public static readonly DependencyProperty IsSuggestionListOpenProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register(nameof(IsSuggestionListOpen), false);

    public static readonly DependencyProperty SuggestionListDirectionProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register(nameof(SuggestionListDirection), SuggestionListDirection.Auto, (obj, args) => obj.OnSuggestionListDirectionChanged());

    public static readonly DependencyProperty SubmitCommandProperty = DependencyPropertyHelper<AutoSuggestionBox>
        .Register<ICommand?>(nameof(SubmitCommand), null);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public string PlaceholderText { get => (string)GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    public DataTemplate? ItemTemplate { get => (DataTemplate?)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

    public bool IsSuggestionListOpen { get => (bool)GetValue(IsSuggestionListOpenProperty); private set => SetValue(IsSuggestionListOpenProperty, value); }

    public SuggestionListDirection SuggestionListDirection { get => (SuggestionListDirection)GetValue(SuggestionListDirectionProperty); set => SetValue(SuggestionListDirectionProperty, value); }

    public ICommand? SubmitCommand { get => (ICommand?)GetValue(SubmitCommandProperty); set => SetValue(SubmitCommandProperty, value); }

    private bool HasSuggestinos => ItemsSource is not null && ItemsSource.Cast<object>().Any();

    private bool ignoreNextTextChange = false;

    public AutoSuggestionBox()
    {
        this.InitializeComponent();
    }

    private void AutoCompleteBox_Loaded(object sender, RoutedEventArgs e)
    {
        textBox.ApplyTemplate();
        var queryButton = (Button)textBox.FindChild("QueryButton")!;
        queryButton.Click += QueryButton_Click;
        queryButton.Content = new FontIcon() { FontSize = 12, FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE8FB" };
        var cursorProperty = queryButton.GetType().GetProperty(nameof(ProtectedCursor), BindingFlags.NonPublic | BindingFlags.Instance)!;
        cursorProperty.SetValue(queryButton, InputSystemCursor.Create(InputSystemCursorShape.Arrow));
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = textBox.Text;

        if (ignoreNextTextChange)
        {
            ignoreNextTextChange = false;
            return;
        }

        SuggestionsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (HasSuggestinos)
        {
            UpdatePopupOffset();
            IsSuggestionListOpen = true;
        }

        SuggestionsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs args)
    {
        IsSuggestionListOpen = false;
    }

    private void OnItemsSourceChanged()
    {
        UpdateItemsSource();

        if (textBox.FocusState != FocusState.Unfocused)
        {
            UpdatePopupOffset();
            IsSuggestionListOpen = HasSuggestinos;
        }
    }

    private void OnSuggestionListDirectionChanged()
    {
        UpdateItemsSource();
        UpdatePopupOffset();
    }

    private void UpdateItemsSource()
    {
        if (IsSuggestionListDirectionTop())
        {
            suggestionsList.ItemsSource = ItemsSource?.Cast<object>().Reverse();
        }
        else
        {
            suggestionsList.ItemsSource = ItemsSource;
        }
    }

    private void UpdatePopupOffset()
    {
        if (IsSuggestionListDirectionTop())
        {
            popup.VerticalOffset = -suggestionsList.ActualHeight;
        }
        else
        {
            popup.VerticalOffset = textBox.ActualHeight;
        }
    }

    private bool IsSuggestionListDirectionTop()
    {
        if (SuggestionListDirection == SuggestionListDirection.Auto)
        {
            Point position = textBox.TransformToVisual(XamlRoot.Content).TransformPoint(new Point(0, 0));
            return position.Y + suggestionsList.ActualSize.Y > XamlRoot.Content.ActualSize.Y;
        }
        return SuggestionListDirection == SuggestionListDirection.Top;
    }

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            IsSuggestionListOpen = false;
            Submit();
        }
        else if (e.Key == VirtualKey.Down && IsSuggestionListOpen)
        {
            e.Handled = true;

            if (suggestionsList.SelectedIndex < suggestionsList.Items.Count - 1)
            {
                suggestionsList.SelectedIndex++;
            }
            else
            {
                suggestionsList.SelectedIndex = -1;
            }

            OnSelectedSuggestionChanged();
        }
        else if (e.Key == VirtualKey.Up && IsSuggestionListOpen)
        {
            e.Handled = true;

            if (suggestionsList.SelectedIndex > -1)
            {
                suggestionsList.SelectedIndex--;
            }
            else
            {
                suggestionsList.SelectedIndex = suggestionsList.Items.Count - 1;
            }

            OnSelectedSuggestionChanged();
        }
        else if (e.Key == VirtualKey.Escape && IsSuggestionListOpen)
        {
            e.Handled = IsSuggestionListOpen;
            IsSuggestionListOpen = false;
        }
    }

    private void OnSelectedSuggestionChanged()
    {
        ignoreNextTextChange = true;
        textBox.Text = suggestionsList.SelectedItem?.ToString() ?? Text;
    }

    private void SuggestionsList_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePopupOffset();
    }

    private void SuggestionsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        IsSuggestionListOpen = false;
        ignoreNextTextChange = true;
        Text = e.ClickedItem.ToString() ?? "";
        Submit();
    }

    private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        suggestionsList.MinWidth = ActualWidth;
    }

    private void QueryButton_Click(object sender, RoutedEventArgs e)
    {
        Submit();
    }

    private void Submit()
    {
        IsSuggestionListOpen = false;

        if (SubmitCommand is not null && SubmitCommand.CanExecute(Text))
        {
            SubmitCommand.Execute(Text);
        }

        Submitted?.Invoke(this, EventArgs.Empty);
    }
}
