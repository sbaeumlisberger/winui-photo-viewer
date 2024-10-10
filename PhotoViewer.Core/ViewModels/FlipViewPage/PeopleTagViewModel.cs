using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Foundation;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTagViewModel : ObservableObject
{
    public required string Name { get; init; }

    public required Rect FaceBox { get; init; }

    public double FaceBoxCenterX => FaceBox.X + FaceBox.Width / 2;

    public required bool IsVisible { get; set; }

    public required float UIScaleFactor { get; set; } = 1;
}
