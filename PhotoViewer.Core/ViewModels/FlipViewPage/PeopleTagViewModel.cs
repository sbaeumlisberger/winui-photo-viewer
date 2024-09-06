using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Foundation;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTagViewModel : ObservableObject
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
