using Microsoft.UI.Xaml;
using System;

namespace PhotoViewerApp.Utils;

internal class WindowsManger
{

    [ThreadStatic]
    private static Window? window;

    public static Window GetForCurrentThread()
    {
        return window ?? throw new Exception("No window assigned to current thread.");
    }

    public static void SetForCurrentThread(Window window)
    {
        WindowsManger.window = window;
        window.Closed += Window_Closed;
    }

    private static void Window_Closed(object sender, WindowEventArgs args)
    {
        window = null;
    }
}
