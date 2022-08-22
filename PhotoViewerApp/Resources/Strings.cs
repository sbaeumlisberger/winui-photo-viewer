
/* AUTO GENERATED CODE - DO NOT CHANGE */ 

using Windows.ApplicationModel.Resources;
using System.Reflection;

namespace PhotoViewerApp.Resources {

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
        /// Nothing to show
        /// </summary>
		public static string NoItemsMessage => Get("NoItemsMessage");	
		/// <summary>
        /// Open folder
        /// </summary>
		public static string OpenFolderButton => Get("OpenFolderButton");	

		private Strings() {}

	}

}
