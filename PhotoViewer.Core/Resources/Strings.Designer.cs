﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PhotoViewer.Core.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PhotoViewer.Core.Resources.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to add people tag.
        /// </summary>
        public static string AddPeopleTagErrorDialog_Title {
            get {
                return ResourceManager.GetString("AddPeopleTagErrorDialog_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following files could not be deleted:.
        /// </summary>
        public static string DeleteFilesErrorDialog_Message {
            get {
                return ResourceManager.GetString("DeleteFilesErrorDialog_Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not delete files.
        /// </summary>
        public static string DeleteFilesErrorDialog_Title {
            get {
                return ResourceManager.GetString("DeleteFilesErrorDialog_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No place taken specified.
        /// </summary>
        public static string MetadataPanel_LocationPlaceholder {
            get {
                return ResourceManager.GetString("MetadataPanel_LocationPlaceholder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Different values.
        /// </summary>
        public static string MetadataPanel_LocationPlaceholderMultipleValues {
            get {
                return ResourceManager.GetString("MetadataPanel_LocationPlaceholderMultipleValues", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There already exists a people tag with the name &quot;{0}&quot;.
        /// </summary>
        public static string PeopleTagAlreadyExistingDialog_Message {
            get {
                return ResourceManager.GetString("PeopleTagAlreadyExistingDialog_Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Person already tagged.
        /// </summary>
        public static string PeopleTagAlreadyExistingDialog_Title {
            get {
                return ResourceManager.GetString("PeopleTagAlreadyExistingDialog_Title", resourceCulture);
            }
        }
    }
}
