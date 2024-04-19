using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhotoViewer.App.Utils;

public class ListViewExtension
{

    public static readonly DependencyProperty AutoSelectProperty = DependencyPropertyHelper<ListViewExtension>
        .RegisterAttached(false, MenuFlyout_AutoSelectChanged);

    public static bool GetAutoSelect(MenuFlyout element) => (bool)element.GetValue(AutoSelectProperty);
    public static void SetAutoSelect(MenuFlyout element, bool value) => element.SetValue(AutoSelectProperty, value);

    private ListViewExtension() { }

    private static void MenuFlyout_AutoSelectChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var menuFlyout = (MenuFlyout)sender;

        if (args.NewValue is true)
        {
            menuFlyout.Opening += MenuFlyout_Opening;
        }
        else
        {
            menuFlyout.Opening -= MenuFlyout_Opening;
        }
    }

    private static void MenuFlyout_Opening(object? sender, object e)
    {
        var menuFlyout = (MenuFlyout)sender!;

        var listView = VisualTreeUtil.FindParent<ListView>(menuFlyout.Target)!;

        if (!listView.SelectedItems.Contains(menuFlyout.Target.DataContext))
        {
            listView.SelectedItems.Clear();
            listView.SelectedItems.Add(menuFlyout.Target.DataContext);
        }
    }
}