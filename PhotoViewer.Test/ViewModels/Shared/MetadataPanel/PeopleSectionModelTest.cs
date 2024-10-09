using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using NSubstitute;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System.Collections.Immutable;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class PeopleSectionModelTest
{
    private const string Name1 = "FirstName1 LastName1";
    private const string Name2 = "FirstName2 LastName2";
    private const string Name3 = "FirstName3 LastName3";

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly ISuggestionsService suggestionsService = Substitute.For<ISuggestionsService>();

    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly PeopleSectionModel peopleSectionModel;

    public PeopleSectionModelTest()
    {
        peopleSectionModel = new PeopleSectionModel(messenger, metadataService, suggestionsService, dialogService, backgroundTaskService, false);
    }

    [Fact]
    public void UpdatesPeopleList_WhenFilesChanged()
    {
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadata = new[]
        {
            CreateMetadataView(),
            CreateMetadataView(Name1),
            CreateMetadataView(Name2),
            CreateMetadataView(Name1, Name3)
        };

        peopleSectionModel.UpdateFilesChanged(files, metadata);

        var people = peopleSectionModel.People;

        Assert.Equal(3, people.Count);

        Assert.Equal(Name1, people[0].Value);
        Assert.Equal(Name1, people[0].ShortValue);
        Assert.Equal(2, people[0].Count);
        Assert.Equal(4, people[0].Total);

        Assert.Equal(Name2, people[1].Value);
        Assert.Equal(Name2, people[1].ShortValue);
        Assert.Equal(1, people[1].Count);
        Assert.Equal(4, people[1].Total);

        Assert.Equal(Name3, people[2].Value);
        Assert.Equal(Name3, people[2].ShortValue);
        Assert.Equal(1, people[2].Count);
        Assert.Equal(4, people[2].Total);
    }


    [Fact]
    public void UpdatesPeopleList_WhenMetadataModified()
    {
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadataFile1 = CreateMetadataView(Name1);
        var metadataFile2 = CreateMetadataView(Name2);
        var metadata = new[] { metadataFile1, metadataFile2 };
        peopleSectionModel.UpdateFilesChanged(files, metadata);

        UpdateMetadataView(metadataFile1, Name1, Name3);
        peopleSectionModel.UpdateMetadataModified(MetadataProperties.People);

        var people = peopleSectionModel.People;

        Assert.Equal(3, people.Count);

        Assert.Equal(Name3, people[1].Value);
        Assert.Equal(Name3, people[1].ShortValue);
        Assert.Equal(1, people[1].Count);
        Assert.Equal(2, people[1].Total);
    }

    [Fact]
    public void AddPersonCommandCanNotExecute_WhenAutoSuggestBoxTextEmtpty()
    {
        Assert.False(peopleSectionModel.AddPersonCommand.CanExecute(null));
    }

    [Fact]
    public void AddPersonCommandCanNotExecute_WhenAutoSuggestBoxTextIsWhitespace()
    {
        peopleSectionModel.AutoSuggestBoxText = "   ";
        Assert.False(peopleSectionModel.AddPersonCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddPersonCommandAddsPeopleTagToFiles()
    {
        var files = ImmutableList.Create(
            MockBitmapFileInfo(),
            MockBitmapFileInfo(Name1),
            MockBitmapFileInfo(Name2),
            MockBitmapFileInfo(Name3));
        peopleSectionModel.UpdateFilesChanged(files, Substitute.For<IReadOnlyList<MetadataView>>());
        peopleSectionModel.AutoSuggestBoxText = Name1;
        var messageCapture = TestUtils.CaptureMessage<MetadataModifiedMessage>(messenger);

        await peopleSectionModel.AddPersonCommand.ExecuteAsync(null);

        await VerifyReceivedWriteMetadataAsync(files[0], Name1);
        await VerifyNotReceivedWriteMetadataAsync(files[1]);
        await VerifyReceivedWriteMetadataAsync(files[2], Name2, Name1);
        await VerifyReceivedWriteMetadataAsync(files[3], Name3, Name1);
        Assert.Empty(peopleSectionModel.AutoSuggestBoxText);
        await suggestionsService.Received().AddSuggestionAsync(Name1);
        Assert.Equal(3, messageCapture.Message.Files.Count);
        Assert.Equal(MetadataProperties.People, messageCapture.Message.MetadataProperty);
    }

    [Fact]
    public async Task RemovePeopleTagCommandRemovesPeopleTagFromFiles()
    {
        var files = ImmutableList.Create(
            MockBitmapFileInfo(),
            MockBitmapFileInfo(Name1),
            MockBitmapFileInfo(Name2),
            MockBitmapFileInfo(Name1, Name3));
        peopleSectionModel.UpdateFilesChanged(files, Substitute.For<IReadOnlyList<MetadataView>>());
        var messageCapture = TestUtils.CaptureMessage<MetadataModifiedMessage>(messenger);

        peopleSectionModel.SelectedPeopleNames = [Name1];
        await peopleSectionModel.RemovePeopleTagCommand.ExecuteAsync(null);

        await VerifyNotReceivedWriteMetadataAsync(files[0]);
        await VerifyReceivedWriteMetadataAsync(files[1], new string[0]);
        await VerifyNotReceivedWriteMetadataAsync(files[2]);
        await VerifyReceivedWriteMetadataAsync(files[3], Name3);
        Assert.Equal(2, messageCapture.Message.Files.Count);
        Assert.Equal(MetadataProperties.People, messageCapture.Message.MetadataProperty);
    }

    [Fact]
    public void TagPeopleOnPhotoGetsChecked_WhenMessageReceived()
    {
        var peopleSectionModel = new PeopleSectionModel(messenger, metadataService, suggestionsService, dialogService, backgroundTaskService, true);

        messenger.Send(new SetTagPeopleToolActiveMessage(true));

        Assert.True(peopleSectionModel.IsTagPeopleOnPhotoButtonChecked);
    }

    private MetadataView CreateMetadataView(params string[] peopleNames)
    {
        var peopleTags = peopleNames.Select(name => new PeopleTag(name)).ToList();

        return new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.People.Identifier, peopleTags }
        });
    }

    private void UpdateMetadataView(MetadataView metadataView, params string[] peopleNames)
    {
        var peopleTags = peopleNames.Select(name => new PeopleTag(name)).ToList();
        metadataView.Source[MetadataProperties.People.Identifier] = peopleTags;
    }

    private IBitmapFileInfo MockBitmapFileInfo(params string[] peopleNames)
    {
        var file = Substitute.For<IBitmapFileInfo>();
        var peopleTags = peopleNames.Select(name => new PeopleTag(name)).ToList();
        metadataService.GetMetadataAsync(file, MetadataProperties.People).Returns(peopleTags);
        return file;
    }

    private async Task VerifyNotReceivedWriteMetadataAsync(IBitmapFileInfo file)
    {
        await metadataService.DidNotReceive().WriteMetadataAsync(file, MetadataProperties.People, Arg.Any<IList<PeopleTag>>());
    }

    private async Task VerifyReceivedWriteMetadataAsync(IBitmapFileInfo file, params string[] peopleNames)
    {
        var peopleTags = peopleNames.Select(name => new PeopleTag(name)).ToList();
        await metadataService.Received().WriteMetadataAsync(
            file,
            MetadataProperties.People,
            Arg.Is<IList<PeopleTag>>(arg => arg.SequenceEqual(peopleTags)));
    }

}
