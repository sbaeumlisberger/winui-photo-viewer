using CommunityToolkit.Mvvm.ComponentModel;
using MetadataAPI.Data;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels;

public class PeopleTagViewModel : ObservableObject
{
    public string Name { get; }

    public FaceRect Rectangle { get; }

    public bool IsVisible { get; set; }

    public PeopleTagViewModel(bool isVisible, string name, FaceRect rectangle)
    {
        IsVisible = isVisible;
        Name = name;
        Rectangle = rectangle;
    }
}
