using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Resources;
using System;
using System.Threading.Tasks;

namespace PhotoViewerApp.Views;

public class YesNoDialog : ContentDialog
{
    public bool IsRemember => checkBox?.IsChecked ?? false;

    private readonly CheckBox? checkBox;

    public YesNoDialog(string title, string message, string rememberText = "")
    {
        Title = title;

        PrimaryButtonText = Strings.YesNoDialog_Yes;
        CloseButtonText = Strings.YesNoDialog_No;
        DefaultButton = ContentDialogButton.Primary;

        if (rememberText != "")
        {
            checkBox = new CheckBox()
            {
                Content = rememberText,
                Margin = new Thickness(4, 4, 0, 0)
            };

            Content = new StackPanel()
            {
                Children = 
                {
                    new TextBlock() { Text = message },
                    checkBox,
                }
            };
        }
        else
        {
            Content = message;
        }
    }

    public new async Task<bool> ShowAsync()
    {
        return await base.ShowAsync() == ContentDialogResult.Primary;
    }
}

