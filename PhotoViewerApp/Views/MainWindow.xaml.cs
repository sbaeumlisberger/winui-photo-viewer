using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerApp.Views;
using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using Microsoft.Graphics.Canvas.UI.Composition;
using PhotoViewerApp.Services;

namespace PhotoViewerApp;

public sealed partial class MainWindow : Window
{
    public IDialogService DialogService { get; }

    public MainWindow(IMessenger messenger)
    {     
        this.InitializeComponent();        
        DialogService = new DialogService(this);
        messenger.Subscribe<NavigateToPageMessage>((msg) => frame.Navigate(msg.PageType));
    }
}
