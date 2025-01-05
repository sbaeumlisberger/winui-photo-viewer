using PhotoViewer.Core.Utils;
using Windows.Foundation;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTagViewModel : ObservableObjectBase
{
    public required string Name { get; init; }

    public required Rect FaceBox { get; init; }

    public double FaceBoxCenterX => FaceBox.X + FaceBox.Width / 2;

    public required partial bool IsVisible { get; set; }

    public required partial float UIScaleFactor { get; set; } = 1;
}
