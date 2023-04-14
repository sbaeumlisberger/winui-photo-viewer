using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using Moq;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;
using Xunit;

namespace PhotoViewer.Test.ViewModels;

public class DateTakenSectionModelTest
{

    private readonly SequentialTaskRunner writeFilesRunner = new SequentialTaskRunner();
    private readonly IMessenger messenger = new StrongReferenceMessenger();
    private readonly IMetadataService metadataService = Mock.Of<IMetadataService>();
    private readonly IDialogService dialogService = Mock.Of<IDialogService>();
    private readonly DateTakenSectionModel dateTakenSectionModel;


    public DateTakenSectionModelTest()
    {
        dateTakenSectionModel = new DateTakenSectionModel(writeFilesRunner, messenger, metadataService, dialogService);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateFilesChanged_SingleFile(bool intial)
    {
        if (!intial) { IntialUpdate(); }
        var mediaFile = Mock.Of<IBitmapFileInfo>();
        var dateTaken = new DateTime(2023, 03, 02, 19, 10, 35);
        var metadata = new[] { CreateMetadataView(dateTaken) };

        dateTakenSectionModel.UpdateFilesChanged(new[] { mediaFile }, metadata);

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
        if(!intial) { IntialUpdate(); }
        var mediaFile = Mock.Of<IBitmapFileInfo>();
        var metadata = new[] { CreateMetadataView(null) };

        dateTakenSectionModel.UpdateFilesChanged(new[] { mediaFile }, metadata);

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
        var files = Mock.Of<IReadOnlyList<IBitmapFileInfo>>();
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

        var files = Mock.Of<IReadOnlyList<IBitmapFileInfo>>();
        
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
        if(!intial) { IntialUpdate(); }
        var files = Mock.Of<IReadOnlyList<IBitmapFileInfo>>();
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
    public void MetadataIsWrittenWhenDateOrTimeChanged()
    {
        IntialUpdate();

        dateTakenSectionModel.Date = new DateTime(2023, 02, 28);

        Mock.Get(metadataService).Verify(m => m.WriteMetadataAsync(
            It.IsAny<IBitmapFileInfo>(), MetadataProperties.DateTaken, new DateTime(2023, 02, 28, 11, 38, 12)));

        dateTakenSectionModel.Time = new TimeSpan(07, 19, 46);

        Mock.Get(metadataService).Verify(m => m.WriteMetadataAsync(
            It.IsAny<IBitmapFileInfo>(), MetadataProperties.DateTaken, new DateTime(2023, 02, 28, 07, 19, 46)));
    }

    [Fact]
    public async Task AddDateTakenCommand_Execute()
    {
        var mediaFile = Mock.Of<IBitmapFileInfo>();
        var metadata = new[] { CreateMetadataView(null) };
        dateTakenSectionModel.UpdateFilesChanged(new[] { mediaFile }, metadata);

        await dateTakenSectionModel.AddDateTakenCommand.ExecuteAsync(null);

        Assert.True(dateTakenSectionModel.IsSingleValue);
        Assert.False(dateTakenSectionModel.IsNotPresent);
        Assert.False(dateTakenSectionModel.IsRange);
        Assert.NotNull(dateTakenSectionModel.Date);
        Assert.NotNull(dateTakenSectionModel.Time);
        Assert.Empty(dateTakenSectionModel.RangeText);
        Assert.False(dateTakenSectionModel.AddDateTakenCommand.CanExecute(null));
        Assert.True(dateTakenSectionModel.ShiftDateTakenCommand.CanExecute(null));
        Mock.Get(metadataService).Verify(m => m.WriteMetadataAsync(
            mediaFile, MetadataProperties.DateTaken, It.IsAny<DateTime?>()));
    }

    [Fact]
    public void UpdateMetadataModified()
    {
        var mediaFile = Mock.Of<IBitmapFileInfo>();
        var dateTaken = new DateTime(2021, 06, 17, 11, 38, 12);
        var metadataSource = new Dictionary<string, object?>
        {
            { MetadataProperties.DateTaken.Identifier, dateTaken }
        };
        var metadata = new[] { new MetadataView(metadataSource) };
        dateTakenSectionModel.UpdateFilesChanged(new[] { mediaFile }, metadata);
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
        dateTakenSectionModel.UpdateFilesChanged(new[] { Mock.Of<IBitmapFileInfo>() }, new[] { new MetadataView(metadata) });
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
        Mock.Get(metadataService).Verify(
            m => m.WriteMetadataAsync(It.IsAny<IBitmapFileInfo>(), MetadataProperties.DateTaken, It.IsAny<DateTime?>()),
            Times.Never);
    }

}
