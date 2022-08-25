using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace PhotoViewerApp.Utils;

internal class ColorProfileProvider
{
    private static readonly uint EDD_GET_DEVICE_INTERFACE_NAME = 1;

    private static ColorManagementProfile? colorProfile;

    public static ColorManagementProfile? GetColorProfile()
    {
        return colorProfile;
    }

    // TODO handle no profile assigned
    // TODO handle profile not activated
    // TODO window moved
    public static void Initialize(Window window)
    {
        try
        {
            unsafe
            {
                IntPtr windowHandle = WindowNative.GetWindowHandle(window);

                var monitor = Windows.Win32.PInvoke.MonitorFromWindow(new Windows.Win32.Foundation.HWND(windowHandle), Windows.Win32.Graphics.Gdi.MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

                var monitorInfo = new MonitorInfoEx();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MonitorInfoEx));

                GetMonitorInfo(monitor, ref monitorInfo);

                string deviceName = new string(monitorInfo.szDevice.TakeWhile(c => c != 0).ToArray());

                Log.Info("deviceName: " + deviceName);

                var displayDevice = default(Windows.Win32.Graphics.Gdi.DISPLAY_DEVICEW);
                displayDevice.cb = (uint)sizeof(Windows.Win32.Graphics.Gdi.DISPLAY_DEVICEW);

                ThrowOnError(Windows.Win32.PInvoke.EnumDisplayDevices(deviceName, 0, ref displayDevice, EDD_GET_DEVICE_INTERFACE_NAME));

                string deviceKey = displayDevice.DeviceKey.ToString();

                Log.Info("deviceKey: " + deviceKey);

                ThrowOnError(Windows.Win32.PInvoke.WcsGetDefaultColorProfileSize(
                  Windows.Win32.UI.ColorSystem.WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER,
                  deviceKey,
                  Windows.Win32.UI.ColorSystem.COLORPROFILETYPE.CPT_ICC,
                  Windows.Win32.UI.ColorSystem.COLORPROFILESUBTYPE.CPST_NONE,
                  0,
                  out uint bufferSize));

                IntPtr ptS = Marshal.AllocHGlobal((int)bufferSize);
                char* ptsPt = (char*)ptS.ToPointer();

                Windows.Win32.Foundation.PWSTR profileName = new Windows.Win32.Foundation.PWSTR(ptsPt);

                ThrowOnError(Windows.Win32.PInvoke.WcsGetDefaultColorProfile(
                    Windows.Win32.UI.ColorSystem.WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER,
                    deviceKey,
                    Windows.Win32.UI.ColorSystem.COLORPROFILETYPE.CPT_ICC,
                    Windows.Win32.UI.ColorSystem.COLORPROFILESUBTYPE.CPST_NONE,
                    0,
                    bufferSize,
                    profileName));

                Log.Info(profileName.ToString());

                string path = Path.Combine(Environment.SystemDirectory, "spool/drivers/color", profileName.ToString());

                Log.Info(path);

                var bytes = File.ReadAllBytes(path);

                colorProfile = ColorManagementProfile.CreateCustom(bytes);

                Log.Info("CP: " + colorProfile);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load color profile of display.", ex);
        }
    }

    private static void ThrowOnError(Windows.Win32.Foundation.BOOL result)
    {
        if (result.Value != 1)
        {
            throw new Exception();
        }
    }

    [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern bool GetMonitorInfo(IntPtr hmonitor, ref MonitorInfoEx info);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
    internal struct MonitorInfoEx
    {
        public uint cbSize;
        public Windows.Win32.Foundation.RECT rcMonitor;
        public Windows.Win32.Foundation.RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] szDevice;
    }

}
