
/* AUTO GENERATED CODE - DO NOT CHANGE */

using System.Reflection;
using Windows.ApplicationModel.Resources;

namespace PhotoViewerApp.Resources;

public class Strings
{

    private static ResourceLoader resourceLoader;

    static Strings()
    {
        var declaringAssembly = typeof(Strings).Assembly;
        string name = typeof(Strings).Name;

        string executingAssemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        string declaringAssemblyName = declaringAssembly.GetName().Name!;

        string path;
        if (declaringAssemblyName == executingAssemblyName)
        {
            path = name;
        }
        else
        {
            path = declaringAssemblyName + "/" + name;
        }

        resourceLoader = ResourceLoader.GetForViewIndependentUse(path);
    }

    protected static string Get(string name)
    {
        return resourceLoader.GetString(name);
    }

    /// <summary>
    /// Adobe RGB color space
    /// </summary>
    public static string DetailsBar_ColorSpaceAdobeRGB => Get("DetailsBar_ColorSpaceAdobeRGB");
    /// <summary>
    /// sRGB color space
    /// </summary>
    public static string DetailsBar_ColorSpaceSRGB => Get("DetailsBar_ColorSpaceSRGB");
    /// <summary>
    /// Unknown color space
    /// </summary>
    public static string DetailsBar_ColorSpaceUnknown => Get("DetailsBar_ColorSpaceUnknown");
    /// <summary>
    /// no information available
    /// </summary>
    public static string DetailsBar_NoInformationAvailable => Get("DetailsBar_NoInformationAvailable");
    /// <summary>
    /// Close
    /// </summary>
    public static string MetadataPanel_CloseButtonToolTip => Get("MetadataPanel_CloseButtonToolTip");
    /// <summary>
    /// Metadata
    /// </summary>
    public static string MetadataPanel_Title => Get("MetadataPanel_Title");
    /// <summary>
    /// Nothing to show
    /// </summary>
    public static string NoItemsMessage => Get("NoItemsMessage");
    /// <summary>
    /// Open folder
    /// </summary>
    public static string OpenFolderButton => Get("OpenFolderButton");
    /// <summary>
    /// Back
    /// </summary>
    public static string SettingsPage_BackButtonTooltip => Get("SettingsPage_BackButtonTooltip");
    /// <summary>
    /// Settings
    /// </summary>
    public static string SettingsPage_Title => Get("SettingsPage_Title");

    private Strings() { }

}

