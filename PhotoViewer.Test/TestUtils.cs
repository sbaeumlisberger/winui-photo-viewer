using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using NSubstitute;
using NSubstitute.Core;
using PhotoViewer.Core.Models;
using System.ComponentModel;
using Windows.Storage;
using Xunit;

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

    internal class Capture<TMessage>
    {
        public TMessage Message
        {
            get => message ?? throw new Exception("Message was not captured");
            set => message = value;
        }

        public bool IsMessageCaptured => message != null;

        private TMessage? message;
    };

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

    internal static void RaisePropertyChanged(this INotifyPropertyChanged obj, string propertyName)
    {
        obj.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(obj, new PropertyChangedEventArgs(propertyName));
    }

    internal static List<string?> CapturePropertyChangedEvents(INotifyPropertyChanged obj)
    {
        var events = new List<string?>();
        obj.PropertyChanged += (_, e) => events.Add(e.PropertyName);
        return events;
    }

    internal static ConfiguredCall ReturnsAsyncOperation<T>(this object mock, T returnValue)
    {
        return mock.Returns(Task.FromResult(returnValue).AsAsyncOperation());
    }

    internal static void CheckSynchronizationContextOfPropertyChangedEvents(INotifyPropertyChanged obj, SynchronizationContext expectedSynchronizationContext)
    {
        obj.PropertyChanged += (s, e) =>
        {
            if (SynchronizationContext.Current != expectedSynchronizationContext)
            {
                Assert.Fail($"Property changed event for \"{e.PropertyName}\" was not invoked on expected synchronization context.");
            }
        };
    }

    internal static IDisposable RegisterLogger(ILogger logger)
    {
        if (Log.Logger != AsyncLocalLogger.Instance)
        {
            Log.Configure(AsyncLocalLogger.Instance);
        }

        var previousLogger = AsyncLocalLogger.Instance.Logger;

        AsyncLocalLogger.Instance.Logger = logger;

        return new DelegatingDisposable(() => AsyncLocalLogger.Instance.Logger = previousLogger);
    }
}