using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.Core.Models;
using Windows.Storage;

namespace PhotoViewer.Test;

internal static class TestUtils
{
    private static readonly string TestFoldersPath = Path.Combine(Environment.CurrentDirectory, "TestFolders");

    static TestUtils()
    {
        if (Directory.Exists(TestFoldersPath))
        {
            Directory.Delete(TestFoldersPath, true);
        }
    }

    internal static string CreateTestFolder()
    {
        string path = Path.Combine(TestFoldersPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    internal class Capture<TMessage> { public TMessage? Message { get; set; } };

    internal static Capture<TMessage> CaptureMessage<TMessage>(IMessenger messenger) where TMessage : class
    {
        var capture = new Capture<TMessage>();
        messenger.Register<TMessage>(new object(), (_, msg) => capture.Message = msg);
        return capture;
    }

    internal static IBitmapFileInfo MockBitmapFile(string fileName)
    {
        var storageFileMock = Substitute.For<IStorageFile>();
        storageFileMock.Name.Returns(fileName);
        var bitmapFileInfo = new BitmapFileInfo(storageFileMock);
        return bitmapFileInfo;
    }

}