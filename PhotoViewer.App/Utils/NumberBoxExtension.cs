using Essentials.NET.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhotoViewer.App.Utils;

public class NumberBoxExtension
{
    public static readonly DependencyProperty IsClearButtonHiddenProperty = DependencyPropertyHelper<NumberBoxExtension>
        .RegisterAttached(false, NumberBox_IsClearButtonHiddenChanged);

    public static bool GetIsClearButtonHidden(NumberBox element) => (bool)element.GetValue(IsClearButtonHiddenProperty);
    public static void SetIsClearButtonHidden(NumberBox element, bool value) => element.SetValue(IsClearButtonHiddenProperty, value);

    private NumberBoxExtension() { }

    private static void NumberBox_IsClearButtonHiddenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var textBox = (NumberBox)sender;

        void UpdateClearButton()
        {
            var inputBox = textBox.FindChild("InputBox");

            if (inputBox is null)
            {
                textBox.ApplyTemplate();
                inputBox = textBox.FindChild("InputBox");
            }

            if (inputBox is null)
            {
                Log.Error("input box not found");
                return;
            }

            TextBoxExtension.SetIsClearButtonHidden((TextBox)inputBox, (bool)args.NewValue);
        }

        if (textBox.IsLoaded)
        {
            UpdateClearButton();
        }
        else
        {
            void NumberBox_Loaded(object sender, RoutedEventArgs args)
            {
                UpdateClearButton();
                textBox.Loaded -= NumberBox_Loaded;
            }
            textBox.Loaded += NumberBox_Loaded;
        }
    }
}
