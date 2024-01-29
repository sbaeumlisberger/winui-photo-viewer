using Microsoft.UI.Xaml;
using PhotoViewer.App.Utils;
using System;
using Windows.Graphics.Printing;
using WinUIEx;

namespace PhotoViewer.App.Services;

internal class PrintService
{
    private readonly Window window;

    public PrintService(Window window)
    {
        this.window = window;
    }

    public PrintRegistration RegisterForPrinting(Func<IPrintJob> printJobFactory)
    {
        var printManager = PrintManagerInterop.GetForWindow(window.GetWindowHandle());
        var printRegistration = new PrintRegistration(window.DispatcherQueue, printJobFactory);
        printManager.PrintTaskRequested += printRegistration.OnPrintTaskRequested;
        return printRegistration;
    }

    public void Unregister(PrintRegistration printRegistration)
    {
        var printManager = PrintManagerInterop.GetForWindow(window.GetWindowHandle());
        printManager.PrintTaskRequested -= printRegistration.OnPrintTaskRequested;
    }
}
