using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.Utils;

[NotifyPropertyChanged(ExcludeExplicitProperties = true)]
public class ObservableObject : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}
