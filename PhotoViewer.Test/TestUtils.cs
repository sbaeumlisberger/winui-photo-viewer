using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using Moq;
using NSubstitute;
using NSubstitute.Core;
using PhotoViewer.App.Models;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewer.Test;

internal static class TestUtils
{
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