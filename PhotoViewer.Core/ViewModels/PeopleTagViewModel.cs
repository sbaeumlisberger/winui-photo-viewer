using CommunityToolkit.Mvvm.ComponentModel;
using MetadataAPI.Data;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhotoViewer.Core.ViewModels;

public class PeopleTagViewModel : ObservableObject
{
    public string Name { get; }

    public Rect FaceBox { get; }

    public double FaceBoxCenterX => FaceBox.X + FaceBox.Width / 2;

    public bool IsVisible { get; set; }

    public PeopleTagViewModel(bool isVisible, string name, Rect rectangle)
    {
        IsVisible = isVisible;
        Name = name;
        FaceBox = rectangle;
    }
}
