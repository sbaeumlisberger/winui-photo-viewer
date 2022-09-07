using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml;
using PhotoVieweApp.Services;
using System;
using WinRT.Interop;

namespace PhotoViewerApp.Utils;

internal class ColorProfileProvider
{

    private static ColorManagementProfile? colorProfile;

    public static ColorManagementProfile? GetColorProfile()
    {
        return colorProfile;
    }

    private static readonly IColorProfileService colorProfileService = new ColorProfileService();

    // TODO handle no profile assigned
    // TODO handle profile not activated
    // TODO window moved
    public static void Initialize(Window window)
    {
        IntPtr windowHandle = WindowNative.GetWindowHandle(window);
        colorProfile = colorProfileService.GetColorProfileForWindow(windowHandle);
    }
}
