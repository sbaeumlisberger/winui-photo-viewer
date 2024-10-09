using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System.Collections.Immutable;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class DateTakenSectionModelTest
{
    private readonly IMessenger messenger = new StrongReferenceMessenger();
    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();
    private readonly IDialogService dialogService = Substitute.For<IDialogService>();
    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly DateTakenSectionModel dateTakenSectionModel;

    public DateTakenSectionModelTest()
    {
        dateTakenSectionModel = new DateTakenSectionModel(messenger, metadataService, dialogService, backgroundTaskService);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateFilesChanged_SingleFile(bool intial)
    {
        if (!intial) { IntialUpdate(); }
        var mediaFile = Substitute.For<IBitmapFileInfo>();
        var dateTaken = new DateTime(2023, 03, 02, 19, 10, 35);
        var metadata = new[] { CreateMetadataView(dateTaken) };

        dateTakenSectionModel.UpdateFilesChanged(ImmutableList.Create(mediaFile), metadata);

        Assert.True(dateTakenSectionModel.IsSingleValue);
        Assert.False(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.Equal(dateTaken.Date, dateTakenSectionModel.Date);
        Assert.Equal(dateTaken.TimeOfDay, dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.False(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        VerifyNoMetadataWritten();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateFilesChanged_SingleFile_NotPresent(bool intial)
    {
        if (!intial) { IntialUpdate(); }
        var mediaFile = Substitute.For<IBitmapFileInfo>();
        var metadata = new[] { CreateMetadataView(null) };

        dateTakenSectionModel.UpdateFilesChanged(ImmutableList.Create(mediaFile), metadata);

        Assert.False(dateTakenSectionModel.IsSingleValue);
        Assert.True(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.Null(dateTakenSectionModel.Date);
        Assert.Null(dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.True(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        VerifyNoMetadataWritten();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateFilesChanged_MultipleFiles_SingleValue(bool intial)
    {
        if (!intial) { IntialUpdate(); }
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var dateTaken = new DateTime(2023, 03, 02, 19, 10, 35);
        var metadata = new[]
        {
            CreateMetadataView(dateTaken),
            CreateMetadataView(dateTaken),
            CreateMetadataView(dateTaken),
        };

        dateTakenSectionModel.UpdateFilesChanged(files, metadata);

        Assert.True(dateTakenSectionModel.IsSingleValue);
        Assert.False(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.Equal(dateTaken.Date, dateTakenSectionModel.Date);
        Assert.Equal(dateTaken.TimeOfDay, dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.False(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        VerifyNoMetadataWritten();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateFilesChanged_MultipleFiles_Range(bool intial)
    {
        if (!intial) { IntialUpdate(); }

        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();

        var earliestDateTaken = new DateTime(2022, 04, 07, 06, 17, 57);
        var betweenDateTaken = new DateTime(2022, 12, 27, 12, 13, 38);
        var lastDateTaken = new DateTime(2023, 03, 02, 19, 10, 03);

        var metadata = new[]
        {
            CreateMetadataView(lastDateTaken),
            CreateMetadataView(earliestDateTaken),
            CreateMetadataView(betweenDateTaken),
            CreateMetadataView(null),
        };

        dateTakenSectionModel.UpdateFilesChanged(files, metadata);

        Assert.False(dateTakenSectionModel.IsSingleValue);
        Assert.False(dateTakenSectionModel.IsNotPresent);
        Assert.True(dateTakenSectionModel.IsRange);
        Assert.Null(dateTakenSectionModel.Date);
        Assert.Null(dateTakenSectionModel.Time);
        string expectedRange = earliestDateTaken.ToString("g") + " - " + lastDateTaken.ToString("g");
        Assert.Equal(expectedRange, dateTakenSectionModel.RangeText);
        Assert.False(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        VerifyNoMetadataWritten();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateFilesChanged_MultipleFiles_NotPresent(bool intial)
    {
        if (!intial) { IntialUpdate(); }
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadata = new[]
        {
            CreateMetadataView(null),
            CreateMetadataView(null),
            CreateMetadataView(null),
        };

        dateTakenSectionModel.UpdateFilesChanged(files, metadata);

        Assert.False(dateTakenSectionModel.IsSingleValue);
        Assert.True(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.Null(dateTakenSectionModel.Date);
        Assert.Null(dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.True(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        VerifyNoMetadataWritten();
    }

    [Fact]
    public async Task MetadataIsWrittenWhenDateOrTimeChanged()
    {
        IntialUpdate();

        dateTakenSectionModel.Date = new DateTime(2023, 02, 28);

        await dateTakenSectionModel.LastWriteFilesTask;
        await metadataService.Received().WriteMetadataAsync(
            Arg.Any<IBitmapFileInfo>(), MetadataProperties.DateTaken, new DateTime(2023, 02, 28, 11, 38, 12));

        dateTakenSectionModel.Time = new TimeSpan(07, 19, 46);

        await dateTakenSectionModel.LastWriteFilesTask;
        await metadataService.Received().WriteMetadataAsync(
            Arg.Any<IBitmapFileInfo>(), MetadataProperties.DateTaken, new DateTime(2023, 02, 28, 07, 19, 46));
    }

    [Fact]
    public async Task AddDateTakenCommand_Execute()
    {
        var mediaFile = Substitute.For<IBitmapFileInfo>();
        var metadata = new[] { CreateMetadataView(null) };
        dateTakenSectionModel.UpdateFilesChanged(ImmutableList.Create(mediaFile), metadata);

        await dateTakenSectionModel.AddDateTakenCommand.ExecuteAsync(null);

        Assert.True(dateTakenSectionModel.IsSingleValue);
        Assert.False(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.NotNull(dateTakenSectionModel.Date);
        Assert.NotNull(dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.False(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        await metadataService.Received().WriteMetadataAsync(
            mediaFile, MetadataProperties.DateTaken, Arg.Any<DateTime?>());
    }

    [Fact]
    public void UpdateMetadataModified()
    {
        var mediaFile = Substitute.For<IBitmapFileInfo>();
        var dateTaken = new DateTime(2021, 06, 17, 11, 38, 12);
        var metadataSource = new Dictionary<string, object?>
        {
            { MetadataProperties.DateTaken.Identifier, dateTaken }
        };
        var metadata = new[] { new MetadataView(metadataSource) };
        dateTakenSectionModel.UpdateFilesChanged(ImmutableList.Create(mediaFile), metadata);
        var newDateTaken = new DateTime(2023, 03, 02, 19, 10, 35);
        metadataSource[MetadataProperties.DateTaken.Identifier] = newDateTaken;

        dateTakenSectionModel.UpdateMetadataModified(MetadataProperties.DateTaken);

        Assert.True(dateTakenSectionModel.IsSingleValue);
        Assert.False(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.Equal(newDateTaken.Date, dateTakenSectionModel.Date);
        Assert.Equal(newDateTaken.TimeOfDay, dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.False(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        VerifyNoMetadataWritten();
    }

    private void IntialUpdate()
    {
        var metadata = new Dictionary<string, object?>()
        {
            { MetadataProperties.DateTaken.Identifier, new DateTime(2021, 06, 17, 11, 38, 12) }
        };
        dateTakenSectionModel.UpdateFilesChanged(
            ImmutableList.Create(Substitute.For<IBitmapFileInfo>()),
            new[] { new MetadataView(metadata) });
    }

    private MetadataView CreateMetadataView(DateTime? dateTaken)
    {
        return new MetadataView(new Dictionary<string, object?>
        {
            { MetadataProperties.DateTaken.Identifier, dateTaken }
        });
    }

    private void VerifyNoMetadataWritten()
    {
        metadataService.DidNotReceive()
            .WriteMetadataAsync(Arg.Any<IBitmapFileInfo>(), MetadataProperties.DateTaken, Arg.Any<DateTime?>());
    }

}
