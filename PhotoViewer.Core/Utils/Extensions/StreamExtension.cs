using Windows.Storage.Streams;

namespace PhotoViewer.Core.Utils;

public static class RandomAccessStreamExtension
{

    public static async Task<byte[]> ReadBytesAsync(this IRandomAccessStream stream)
    {
        var bytes = new byte[stream.Size];
        using (var dataReader = new DataReader(stream))
        {
            await dataReader.LoadAsync((uint)stream.Size).AsTask().ConfigureAwait(false);
            dataReader.ReadBytes(bytes);
        }
        return bytes;
    }

}
