using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

public class TextBoxExtension
{
    public static readonly DependencyProperty IsClearButtonHiddenProperty = DependencyPropertyHelper<TextBoxExtension>
        .RegisterAttached(false, TextBox_IsClearButtonHiddenChanged);

    public static bool GetIsClearButtonHidden(FrameworkElement element) => (bool)element.GetValue(IsClearButtonHiddenProperty);
    public static void SetIsClearButtonHidden(FrameworkElement element, bool value) => element.SetValue(IsClearButtonHiddenProperty, value);

    private TextBoxExtension() { }

    private static void TextBox_IsClearButtonHiddenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var textBox = (TextBox)sender;

        void UpdateClearButton()
        {
            var clearButton = textBox.FindChild("DeleteButton")!;

            if (args.NewValue is true)
            {
                clearButton.Width = 0;
                clearButton.MinWidth = 0;
                clearButton.MaxWidth = 0;
            }
            else
            {
                clearButton.Width = double.NaN;
                clearButton.MinWidth = 34;
                clearButton.MaxWidth = double.PositiveInfinity;
            }
        }

        if (textBox.IsLoaded)
        {
            UpdateClearButton();
        }
        else
        {
            void TextBox_Loaded(object sender, RoutedEventArgs args)
            {
                UpdateClearButton();
                textBox.Loaded -= TextBox_Loaded;
            }
            textBox.Loaded += TextBox_Loaded;
        }
    }
}
