using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Threading.Tasks;
using WinUIEx;

namespace PhotoViewer.App.Utils;

interface IColorProfileProvider
{
    event EventHandler? ColorProfileChanged;

    ColorManagementProfile? ColorProfile { get; }
}

internal class ColorProfileProvider : IColorProfileProvider
{
    public static ColorProfileProvider Instance { get; } = new ColorProfileProvider();

    public event EventHandler? ColorProfileChanged;

    public ColorManagementProfile? ColorProfile { get; private set; }

    private DisplayInformation? displayInformation;

    public async Task InitializeAsync(Window window)
    {
        try
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(window.GetWindowHandle());
            displayInformation = DisplayInformation.CreateForWindowId(windowId);
            displayInformation.ColorProfileChanged += DisplayInformation_ColorProfileChanged;
            ColorProfile = await LoadColorProfileAsync(displayInformation);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to initialize ColorProfileProvider", ex);
        }
    }

    private async void DisplayInformation_ColorProfileChanged(DisplayInformation sender, object args)
    {
        Log.Info("Color profile changed");
        ColorProfile = await LoadColorProfileAsync(sender);
        ColorProfileChanged?.Invoke(null, EventArgs.Empty);
    }

    private async Task<ColorManagementProfile?> LoadColorProfileAsync(DisplayInformation displayInformation)
    {
        try
        {
            var colorProfileStream = await displayInformation.GetColorProfileAsync().AsTask().ConfigureAwait(false);
            if (colorProfileStream is null) return null;
            byte[] colorProfileBytes = await colorProfileStream.ReadBytesAsync().ConfigureAwait(false);
            var colorProfile = ColorManagementProfile.CreateCustom(colorProfileBytes);
            return colorProfile;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load display color profile", ex);
            return null;
        }
    }
}
