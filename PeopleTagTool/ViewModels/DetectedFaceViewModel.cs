using LiteDB;
using PeopleTagTool.Models;
using System;
using Windows.Foundation;

namespace PeopleTagTool.ViewModels;

internal class DetectedFaceViewModel(Action<DetectedFaceViewModel> ignoreCallback)
{
    public required DetectedFaceModel FaceModel { get; init; }

    public required IndexedPhoto Photo { get; init; }

    public required string FilePath { get; init; }

    public required Rect FaceBoxInPixels { get; init; }

    public required Rect FaceBoxInPercent { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is DetectedFaceViewModel other
            && FilePath == other.FilePath
            && FaceBoxInPixels.Equals(other.FaceBoxInPixels);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath, FaceBoxInPixels);
    }

    public void IgnoreFace()
    {
        ignoreCallback.Invoke(this);
    }
}
