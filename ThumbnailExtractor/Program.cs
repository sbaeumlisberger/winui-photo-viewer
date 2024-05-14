using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ThumbnailExtractor;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        string filePath = args[0];

        var decoder = BitmapDecoder.Create(new Uri(filePath, UriKind.Relative), BitmapCreateOptions.None, BitmapCacheOption.Default);

        var thumbnail = decoder.Frames[0].Thumbnail;

        var window = new Window
        {
            Title = $"Thumbnail of {Path.GetFileName(filePath)}",
            Content = new Image { Source = thumbnail },
            Width = thumbnail.Width,
            Height = thumbnail.Height
        };

        var app = new Application();
        app.Run(window);
    }
}