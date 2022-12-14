
/* AUTO GENERATED CODE - DO NOT CHANGE */

using Windows.ApplicationModel.Resources;
using System.Reflection;

namespace PhotoViewerApp.Resources;

public class Strings {

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
    /// Compare
    /// </summary>
	public static string FlipViewPageCommandBar_CompareButton => Get("FlipViewPageCommandBar_CompareButton");	
	/// <summary>
    /// Crop
    /// </summary>
	public static string FlipViewPageCommandBar_CropButton => Get("FlipViewPageCommandBar_CropButton");	
	/// <summary>
    /// Delete
    /// </summary>
	public static string FlipViewPageCommandBar_DeleteButton => Get("FlipViewPageCommandBar_DeleteButton");	
	/// <summary>
    /// Start diashow
    /// </summary>
	public static string FlipViewPageCommandBar_DiashowButton => Get("FlipViewPageCommandBar_DiashowButton");	
	/// <summary>
    /// Show metadata
    /// </summary>
	public static string FlipViewPageCommandBar_MetadataButton => Get("FlipViewPageCommandBar_MetadataButton");	
	/// <summary>
    /// Next
    /// </summary>
	public static string FlipViewPageCommandBar_NextButton => Get("FlipViewPageCommandBar_NextButton");	
	/// <summary>
    /// Open folder
    /// </summary>
	public static string FlipViewPageCommandBar_OpenFolderButton => Get("FlipViewPageCommandBar_OpenFolderButton");	
	/// <summary>
    /// Zur Übersicht wechseln
    /// </summary>
	public static string FlipViewPageCommandBar_OverviewButton => Get("FlipViewPageCommandBar_OverviewButton");	
	/// <summary>
    /// Previous
    /// </summary>
	public static string FlipViewPageCommandBar_PreviousButton => Get("FlipViewPageCommandBar_PreviousButton");	
	/// <summary>
    /// Rotate left
    /// </summary>
	public static string FlipViewPageCommandBar_RotateButton => Get("FlipViewPageCommandBar_RotateButton");	
	/// <summary>
    /// Settings
    /// </summary>
	public static string FlipViewPageCommandBar_SettingsButton => Get("FlipViewPageCommandBar_SettingsButton");	
	/// <summary>
    /// All
    /// </summary>
	public static string ItemWithCount_All => Get("ItemWithCount_All");	
	/// <summary>
    /// Delete
    /// </summary>
	public static string MediaFileContextMenu_Delete => Get("MediaFileContextMenu_Delete");	
	/// <summary>
    /// Copy
    /// </summary>
	public static string MediaFileContextMenu_Open => Get("MediaFileContextMenu_Open");	
	/// <summary>
    /// Open in new window
    /// </summary>
	public static string MediaFileContextMenu_OpenInNewWindows => Get("MediaFileContextMenu_OpenInNewWindows");	
	/// <summary>
    /// Open with ...
    /// </summary>
	public static string MediaFileContextMenu_OpenWidth => Get("MediaFileContextMenu_OpenWidth");	
	/// <summary>
    /// Print
    /// </summary>
	public static string MediaFileContextMenu_Print => Get("MediaFileContextMenu_Print");	
	/// <summary>
    /// Properties
    /// </summary>
	public static string MediaFileContextMenu_Properties => Get("MediaFileContextMenu_Properties");	
	/// <summary>
    /// Rotate
    /// </summary>
	public static string MediaFileContextMenu_Rotate => Get("MediaFileContextMenu_Rotate");	
	/// <summary>
    /// Set as ...
    /// </summary>
	public static string MediaFileContextMenu_SetAs => Get("MediaFileContextMenu_SetAs");	
	/// <summary>
    /// Desktop background
    /// </summary>
	public static string MediaFileContextMenu_SetAsDesktopBackground => Get("MediaFileContextMenu_SetAsDesktopBackground");	
	/// <summary>
    /// Lockscreen
    /// </summary>
	public static string MediaFileContextMenu_SetAsLockscreenBackground => Get("MediaFileContextMenu_SetAsLockscreenBackground");	
	/// <summary>
    /// Share
    /// </summary>
	public static string MediaFileContextMenu_Share => Get("MediaFileContextMenu_Share");	
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
    /// Add date taken
    /// </summary>
	public static string MetadataPanel_AddDateTakenButton => Get("MetadataPanel_AddDateTakenButton");	
	/// <summary>
    /// Add keyword
    /// </summary>
	public static string MetadataPanel_AddKeywordPlaceholder => Get("MetadataPanel_AddKeywordPlaceholder");	
	/// <summary>
    /// Add person
    /// </summary>
	public static string MetadataPanel_AddPersonPlaceholder => Get("MetadataPanel_AddPersonPlaceholder");	
	/// <summary>
    /// The selected file does not support metadata
    /// </summary>
	public static string MetadataPanel_AnyFileNotSupported => Get("MetadataPanel_AnyFileNotSupported");	
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
    /// Edit
    /// </summary>
	public static string MetadataPanel_EditLocationButton => Get("MetadataPanel_EditLocationButton");	
	/// <summary>
    /// Import from GPX file
    /// </summary>
	public static string MetadataPanel_ImportFromGpxFileButton => Get("MetadataPanel_ImportFromGpxFileButton");	
	/// <summary>
    /// Keywords
    /// </summary>
	public static string MetadataPanel_KeywordsHeader => Get("MetadataPanel_KeywordsHeader");	
	/// <summary>
    /// Location
    /// </summary>
	public static string MetadataPanel_LocationHeader => Get("MetadataPanel_LocationHeader");	
	/// <summary>
    /// place taken
    /// </summary>
	public static string MetadataPanel_LocationMarker => Get("MetadataPanel_LocationMarker");	
	/// <summary>
    /// No date taken
    /// </summary>
	public static string MetadataPanel_NoDateTakenPresent => Get("MetadataPanel_NoDateTakenPresent");	
	/// <summary>
    /// People
    /// </summary>
	public static string MetadataPanel_PeopleHeader => Get("MetadataPanel_PeopleHeader");	
	/// <summary>
    /// Rating
    /// </summary>
	public static string MetadataPanel_RatingHeader => Get("MetadataPanel_RatingHeader");	
	/// <summary>
    /// Remove keyword
    /// </summary>
	public static string MetadataPanel_RemoveKeyword => Get("MetadataPanel_RemoveKeyword");	
	/// <summary>
    /// Shift date taken
    /// </summary>
	public static string MetadataPanel_ShiftDateTakenButton => Get("MetadataPanel_ShiftDateTakenButton");	
	/// <summary>
    /// Show on map
    /// </summary>
	public static string MetadataPanel_ShowLocationOnMapButton => Get("MetadataPanel_ShowLocationOnMapButton");	
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
    /// Overview
    /// </summary>
	public static string OverviewPage_Title => Get("OverviewPage_Title");	
	/// <summary>
    /// Camera
    /// </summary>
	public static string PropertiesDialog_Camera => Get("PropertiesDialog_Camera");	
	/// <summary>
    /// Close
    /// </summary>
	public static string PropertiesDialog_Close => Get("PropertiesDialog_Close");	
	/// <summary>
    /// Date taken
    /// </summary>
	public static string PropertiesDialog_DateTaken => Get("PropertiesDialog_DateTaken");	
	/// <summary>
    /// Dimensions
    /// </summary>
	public static string PropertiesDialog_Dimensions => Get("PropertiesDialog_Dimensions");	
	/// <summary>
    /// File name
    /// </summary>
	public static string PropertiesDialog_FileName => Get("PropertiesDialog_FileName");	
	/// <summary>
    /// File path
    /// </summary>
	public static string PropertiesDialog_FilePath => Get("PropertiesDialog_FilePath");	
	/// <summary>
    /// File size
    /// </summary>
	public static string PropertiesDialog_FileSize => Get("PropertiesDialog_FileSize");	
	/// <summary>
    /// Aperture
    /// </summary>
	public static string PropertiesDialog_FNumber => Get("PropertiesDialog_FNumber");	
	/// <summary>
    /// Focal length
    /// </summary>
	public static string PropertiesDialog_FocalLength => Get("PropertiesDialog_FocalLength");	
	/// <summary>
    /// ISO
    /// </summary>
	public static string PropertiesDialog_ISO => Get("PropertiesDialog_ISO");	
	/// <summary>
    /// Show in explorer
    /// </summary>
	public static string PropertiesDialog_ShowInExplorer => Get("PropertiesDialog_ShowInExplorer");	
	/// <summary>
    /// Shutter speed
    /// </summary>
	public static string PropertiesDialog_ShutterSpeed => Get("PropertiesDialog_ShutterSpeed");	
	/// <summary>
    /// Properties
    /// </summary>
	public static string PropertiesDialog_Title => Get("PropertiesDialog_Title");	
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
	/// <summary>
    /// No
    /// </summary>
	public static string YesNoDialog_No => Get("YesNoDialog_No");	
	/// <summary>
    /// Yes
    /// </summary>
	public static string YesNoDialog_Yes => Get("YesNoDialog_Yes");	

	private Strings() {}

}

