using Microsoft.UI.Xaml;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Services;

namespace PhotoViewerApp;

public sealed partial class MainWindow : Window
{
    private readonly ViewRegistrations viewRegistrations = new ViewRegistrations();

    public IDialogService DialogService { get; }

    public MainWindow(IMessenger messenger)
    {
        this.InitializeComponent();
        DialogService = new DialogService(this);
        messenger.Subscribe<NavigateToPageMessage>((msg) => frame.Navigate(viewRegistrations.ViewTypeByViewModelType[msg.PageType], msg.Parameter));
    }
}
