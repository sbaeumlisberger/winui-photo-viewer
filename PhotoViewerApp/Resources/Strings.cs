
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
    /// Pause
    /// </summary>
    public static string MediaFlipView_DisableDiashowLoop => Get("MediaFlipView_DisableDiashowLoop");
    /// <summary>
    /// Resume
    /// </summary>
    public static string MediaFlipView_EnableDiashowLoop => Get("MediaFlipView_EnableDiashowLoop");
    /// <summary>
    /// Exit
    /// </summary>
    public static string MediaFlipView_ExitDiashow => Get("MediaFlipView_ExitDiashow");
    /// <summary>
    /// Authors
    /// </summary>
    public static string MetadataPanel_AuthorsHeader => Get("MetadataPanel_AuthorsHeader");
    /// <summary>
    /// Close
    /// </summary>
    public static string MetadataPanel_CloseButtonToolTip => Get("MetadataPanel_CloseButtonToolTip");
    /// <summary>
    /// Copyright
    /// </summary>
    public static string MetadataPanel_CopyrightHeader => Get("MetadataPanel_CopyrightHeader");
    /// <summary>
    /// Date Taken
    /// </summary>
    public static string MetadataPanel_DateTakenHeader => Get("MetadataPanel_DateTakenHeader");
    /// <summary>
    /// Keywords
    /// </summary>
    public static string MetadataPanel_KeywordsHeader => Get("MetadataPanel_KeywordsHeader");
    /// <summary>
    /// Location
    /// </summary>
    public static string MetadataPanel_LocationHeader => Get("MetadataPanel_LocationHeader");
    /// <summary>
    /// People
    /// </summary>
    public static string MetadataPanel_PeopleHeader => Get("MetadataPanel_PeopleHeader");
    /// <summary>
    /// Enter a name
    /// </summary>
    public static string MetadataPanel_PeoplePlaceholder => Get("MetadataPanel_PeoplePlaceholder");
    /// <summary>
    /// Rating
    /// </summary>
    public static string MetadataPanel_RatingHeader => Get("MetadataPanel_RatingHeader");
    /// <summary>
    /// Metadata
    /// </summary>
    public static string MetadataPanel_Title => Get("MetadataPanel_Title");
    /// <summary>
    /// Title
    /// </summary>
    public static string MetadataPanel_TitleHeader => Get("MetadataPanel_TitleHeader");
    /// <summary>
    /// Nothing to show
    /// </summary>
    public static string NoItemsMessage => Get("NoItemsMessage");
    /// <summary>
    /// Open folder
    /// </summary>
    public static string OpenFolderButton => Get("OpenFolderButton");
    /// <summary>
    /// Appearance
    /// </summary>
    public static string SettingsPage_AppearanceSectionTittle => Get("SettingsPage_AppearanceSectionTittle");
    /// <summary>
    /// Show details bar in startup
    /// </summary>
    public static string SettingsPage_AutoShowDetailsBarToogle => Get("SettingsPage_AutoShowDetailsBarToogle");
    /// <summary>
    /// Show metadata view on startup
    /// </summary>
    public static string SettingsPage_AutoShowMetadataPanelToogle => Get("SettingsPage_AutoShowMetadataPanelToogle");
    /// <summary>
    /// Back
    /// </summary>
    public static string SettingsPage_BackButtonTooltip => Get("SettingsPage_BackButtonTooltip");
    /// <summary>
    /// Ask
    /// </summary>
    public static string SettingsPage_DeleteLinkedFilesOption_Ask => Get("SettingsPage_DeleteLinkedFilesOption_Ask");
    /// <summary>
    /// Never
    /// </summary>
    public static string SettingsPage_DeleteLinkedFilesOption_No => Get("SettingsPage_DeleteLinkedFilesOption_No");
    /// <summary>
    /// Always
    /// </summary>
    public static string SettingsPage_DeleteLinkedFilesOption_Yes => Get("SettingsPage_DeleteLinkedFilesOption_Yes");
    /// <summary>
    /// Time span in seconds how long photos are displayed in the diashow
    /// </summary>
    public static string SettingsPage_DiashowTimeLabel => Get("SettingsPage_DiashowTimeLabel");
    /// <summary>
    /// Show delete animation
    /// </summary>
    public static string SettingsPage_ShowDeleteAnimationToggle => Get("SettingsPage_ShowDeleteAnimationToggle");
    /// <summary>
    /// Settings
    /// </summary>
    public static string SettingsPage_Title => Get("SettingsPage_Title");

    private Strings() { }

}

