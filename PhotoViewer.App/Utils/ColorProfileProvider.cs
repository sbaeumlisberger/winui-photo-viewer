using Essentials.NET.Logging;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using PhotoViewer.Core.Utils;
using System;
using System.Threading.Tasks;
using WinUIEx;

namespace PhotoViewer.App.Utils;

interface IColorProfileProvider
{
    event EventHandler? ColorProfileLoaded;

    ColorManagementProfile? ColorProfile { get; }

    bool IsInitialized { get; }
}

internal class ColorProfileProvider : IColorProfileProvider
{
    public static ColorProfileProvider Instance { get; } = new ColorProfileProvider();

    public event EventHandler? ColorProfileLoaded;

    public ColorManagementProfile? ColorProfile { get; private set; }

    public bool IsInitialized { get; private set; } = false;

    private DisplayInformation? displayInformation;

    public async void InitializeAsync(Window window)
    {
        try
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(window.GetWindowHandle());
            displayInformation = DisplayInformation.CreateForWindowId(windowId);
            displayInformation.ColorProfileChanged += DisplayInformation_ColorProfileChanged;
            ColorProfile = await LoadColorProfileAsync(displayInformation);
            IsInitialized = true;
            ColorProfileLoaded?.Invoke(null, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to initialize ColorProfileProvider", ex);
            IsInitialized = true;
            ColorProfileLoaded?.Invoke(null, EventArgs.Empty);
        }
    }

    private async void DisplayInformation_ColorProfileChanged(DisplayInformation sender, object args)
    {
        Log.Info("Color profile changed");
        ColorProfile = await LoadColorProfileAsync(sender);
        ColorProfileLoaded?.Invoke(null, EventArgs.Empty);
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
