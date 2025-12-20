using Microsoft.UI.Xaml;

namespace PeopleTagTool;

public partial class App : Application
{
    public static MainWindow MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow.Activate();
    }
}
