
/* AUTO GENERATED CODE - DO NOT CHANGE */

using Windows.ApplicationModel.Resources;
using System.Reflection;

namespace PhotoViewerCore.Resources;

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
    /// The following files could not be deleted:
    /// </summary>
	public static string DeleteFilesErrorDialog_Message => Get("DeleteFilesErrorDialog_Message");	
	/// <summary>
    /// Could not delete files
    /// </summary>
	public static string DeleteFilesErrorDialog_Title => Get("DeleteFilesErrorDialog_Title");	

	private Strings() {}

}

