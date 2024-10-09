using MetadataAPI;

Console.WriteLine("GPSMetadataFixer started");

string path = @"D:\Dateien\Fotos & Bilder\Fotos Familie";
bool dryRun = true;

var files = Directory.EnumerateFiles(path, "*.jpg", SearchOption.AllDirectories);

int fixedCount = 0;

Parallel.ForEach(files, file =>
{
    try
    {
        using var fileStream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        MetadataEncoder encoder = new MetadataEncoder(fileStream);

        byte? altitudeRef = (byte?)encoder.GetMetadata("System.GPS.AltitudeRef");

        if (altitudeRef is not null && altitudeRef != 0 && altitudeRef != 1)
        {
            Console.WriteLine($"Fix {file} (AltitudeRef={altitudeRef})");

            if (!dryRun)
            {
                encoder.SetMetadata("System.GPS.AltitudeRef", (byte)0);
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


Console.WriteLine($"GPSMetadataFixer finished. {fixedCount} files fixed");