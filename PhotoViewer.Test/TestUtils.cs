using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using Moq;
using NSubstitute;
using NSubstitute.Core;
using PhotoViewer.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

}