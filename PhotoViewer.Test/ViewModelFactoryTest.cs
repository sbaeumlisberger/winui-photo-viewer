using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System.Diagnostics;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test;

public class ViewModelFactoryTest
{
    [Fact]
    public void Initialize()
    {
        var messenger = new StrongReferenceMessenger();

        Assert.Null(ViewModelFactory.Instance);

        var stopwatch = Stopwatch.StartNew();

        ViewModelFactory.Initialize(messenger, new ApplicationSettings(), Substitute.For<IDialogService>());

        stopwatch.Stop();

        Assert.NotNull(ViewModelFactory.Instance);
        //Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 10);
    }

}