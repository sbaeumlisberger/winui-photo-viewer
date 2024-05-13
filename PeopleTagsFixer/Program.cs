using MetadataAPI;
using MetadataAPI.Data;
using OpenCvSharp;

Console.WriteLine("PeopleTagsFixer started");

string path = @"D:\Dateien\Fotos & Bilder\Fotos Familie";
bool dryRun = false;

var files = Directory.EnumerateFiles(path, "*.jpg", SearchOption.AllDirectories);

int fixedCount = 0;

Parallel.ForEach(files, file =>
{
    try
    {
        string facesDirPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "faces"));
        Directory.CreateDirectory(facesDirPath);

        using var fileStream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        MetadataEncoder encoder = new MetadataEncoder(fileStream);

        var peopleTags = encoder.GetProperty(MetadataProperties.People);

        if (peopleTags.Count > 0 && peopleTags.Any(pt => pt.Rectangle is not null))
        {
            var orientation = encoder.GetProperty(MetadataProperties.Orientation);

            bool isOriented = !(orientation == PhotoOrientation.Normal || orientation == PhotoOrientation.Unspecified);

            bool hasInvalidRects = peopleTags.Any(pt => pt.Rectangle is not null && pt.Rectangle.Value == default);

            if (!(isOriented || hasInvalidRects))
            {
                return;
            }

            Console.WriteLine($"Fix {file}");

            var dateTaken = encoder.GetProperty(MetadataProperties.DateTaken);

            foreach (var pt in peopleTags)
            {
                if (pt.Rectangle is null)
                {
                    continue;
                }

                if (pt.Rectangle.Value == default)
                {
                    pt.Rectangle = null;
                }
                else if (isOriented)
                {
                    if (dateTaken < new DateTime(2017, 1, 1)
                        || file.Contains("\\2017-05-13")
                        || file.Contains("\\2017-05-27")
                        || file.Contains("\\2017-07-02")
                        || file.Contains("\\2017-07-09"))
                    {
                        pt.Rectangle = RotateRectPre2017(orientation, pt.Rectangle.Value);
                    }
                    if (dateTaken > new DateTime(2019, 12, 1)
                        || file.Contains("\\2017-01-")
                        || file.Contains("\\2019-07-02")
                        || file.Contains("\\2019-08-05"))
                    {
                        pt.Rectangle = RotateRect(orientation, pt.Rectangle.Value);
                    }

                    using Mat image = Cv2.ImRead(file, ImreadModes.Color | ImreadModes.IgnoreOrientation);

                    var rect = new Rect(
                        (int)(pt.Rectangle.Value.X * image.Width),
                        (int)(pt.Rectangle.Value.Y * image.Height),
                        (int)(pt.Rectangle.Value.Width * image.Width),
                        (int)(pt.Rectangle.Value.Height * image.Height));

                    using Mat faceImage = new Mat(image, rect);

                    string facePath = Path.Combine(facesDirPath, file.Substring(path.Length).Replace("\\", "_").Replace(".JPG", "") + "_" + pt.Name + ".jpg");

                    Cv2.ImWrite(facePath, faceImage);
                }
            }

            if (!dryRun)
            {
                encoder.SetProperty(MetadataProperties.People, peopleTags);
                encoder.EncodeAsync().GetAwaiter().GetResult();
            }

            Interlocked.Increment(ref fixedCount);
        }

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fixing {file}: {ex.Message}");
    }
});

FaceRect RotateRectPre2017(PhotoOrientation orientation, FaceRect rect)
{
    switch (orientation)
    {
        case PhotoOrientation.Rotate90:
        case PhotoOrientation.Rotate270:
            return new FaceRect(1 - rect.X - rect.Width, 1 - rect.Y - rect.Height, rect.Width, rect.Height);
        default:
            throw new NotSupportedException("Unsupported orientation: " + orientation);
    }
}

FaceRect RotateRect(PhotoOrientation orientation, FaceRect rect)
{
    switch (orientation)
    {
        case PhotoOrientation.Rotate90:
            return new FaceRect(1 - rect.Y - rect.Height, rect.X, rect.Height, rect.Width);
        case PhotoOrientation.Rotate180:
            return new FaceRect(1 - rect.X - rect.Width, 1 - rect.Y - rect.Height, rect.Width, rect.Height);
        case PhotoOrientation.Rotate270:
            return new FaceRect(rect.Y, 1 - rect.X - rect.Width, rect.Height, rect.Width);
        default:
            throw new NotSupportedException("Unsupported orientation: " + orientation);

    }
}

Console.WriteLine($"PeopleTagsFixer finished. {fixedCount} files fixed");
