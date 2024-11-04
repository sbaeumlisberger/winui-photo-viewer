using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using MetadataAPI.Data;
using NSubstitute;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Xunit;

namespace PhotoViewer.Test.ViewModels.FlipViewPage;

public class TagPeopleToolModelTest
{

    private readonly TagPeopleToolModel tagPeopleToolModel;

    private readonly IBitmapFileInfo bitmapFile = Substitute.For<IBitmapFileInfo>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly ISuggestionsService suggestionsService = Substitute.For<ISuggestionsService>();

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IFaceDetectionService faceDetectionService = Substitute.For<IFaceDetectionService>();

    private readonly PeopleTag examplePeopleTag1 = new PeopleTag("Person 1", new FaceRect(0.25, 0.25, 0.17, 0.21));
    private readonly PeopleTag examplePeopleTag2 = new PeopleTag("Person 2");
    private readonly PeopleTag examplePeopleTag3 = new PeopleTag("Person 3", new FaceRect(0.45, 0.3, 0.16, 0.19));
    private readonly List<PeopleTag> examplePeopleTags;

    private readonly ICanvasBitmapImageModel exampleBitmapImage = Substitute.For<ICanvasBitmapImageModel>();

    private readonly List<DetectedFaceModel> exampleDetectedFaces;

    private readonly List<Rect> exampleSuggestedFaces;

    public TagPeopleToolModelTest()
    {
        examplePeopleTags = new() { examplePeopleTag1, examplePeopleTag2, examplePeopleTag3 };

        exampleBitmapImage.SizeInDIPs.Returns(new Size(2000, 1000));

        exampleDetectedFaces = new List<DetectedFaceModel>()
        {
            new DetectedFaceModel(new BitmapBounds(460, 220, 380, 420)),
            new DetectedFaceModel(new BitmapBounds(1010, 230, 370, 410)),
            new DetectedFaceModel(new BitmapBounds(1520, 190, 360, 400)),
        };

        exampleSuggestedFaces = new List<Rect>() {
            new Rect(0.23, 0.22, 0.19, 0.42),
            new Rect(0.505, 0.23, 0.185, 0.41),
            new Rect(0.76, 0.19, 0.18, 0.4),
        };

        faceDetectionService.DetectFacesAsync(exampleBitmapImage, Arg.Any<CancellationToken>()).Returns(exampleDetectedFaces);

        tagPeopleToolModel = new TagPeopleToolModel(bitmapFile, messenger, suggestionsService, metadataService, dialogService, faceDetectionService);
    }

    [Fact]
    public async Task InitializeAsync_LoadsPeopleTags()
    {
        messenger.Register<IsTagPeopleToolActiveRequestMessage>(this, (_, msg) => msg.Reply(false));
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(examplePeopleTags);

        await tagPeopleToolModel.InitializeAsync();

        Assert.Equal(2, tagPeopleToolModel.TaggedPeople.Count);

        var peopleTagVM1 = tagPeopleToolModel.TaggedPeople[0];
        Assert.Equal(examplePeopleTag1.Name, peopleTagVM1.Name);
        Assert.Equal(examplePeopleTag1.Rectangle!.Value.ToRect(), peopleTagVM1.FaceBox);
        Assert.False(peopleTagVM1.IsVisible);

        var peopleTagVM2 = tagPeopleToolModel.TaggedPeople[1];
        Assert.Equal(examplePeopleTag3.Name, peopleTagVM2.Name);
        Assert.Equal(examplePeopleTag3.Rectangle!.Value.ToRect(), peopleTagVM2.FaceBox);
        Assert.False(peopleTagVM2.IsVisible);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializeAsync_SetsIsTagPeopleToolActive(bool isActive)
    {
        messenger.Register<IsTagPeopleToolActiveRequestMessage>(this, (_, msg) => msg.Reply(isActive));

        await tagPeopleToolModel.InitializeAsync();

        Assert.Equal(isActive, tagPeopleToolModel.IsTagPeopleToolActive);
    }

    [Fact]
    public async Task ActivationMessage_ShowsTaggedPeople_WhenIsEnabledTrue()
    {
        await InitializeTagPeopleToolModel(peopleTags: examplePeopleTags);
        tagPeopleToolModel.IsEnabled = true;

        messenger.Send(new SetTagPeopleToolActiveMessage(true));
        await tagPeopleToolModel.LastDispatchTask;

        Assert.All(tagPeopleToolModel.TaggedPeople, peopleTagVM => Assert.True(peopleTagVM.IsVisible));
    }

    [Fact]
    public async Task ActivationMessage_DoesNotShowTaggedPeople_WhenIsEnabledFalse()
    {
        await InitializeTagPeopleToolModel(peopleTags: examplePeopleTags);

        messenger.Send(new SetTagPeopleToolActiveMessage(true));

        Assert.All(tagPeopleToolModel.TaggedPeople, peopleTagVM => Assert.False(peopleTagVM.IsVisible));
    }

    [Fact]
    public async Task ActivationMessage_ShowsSuggestedFaces_WhenNoPeopleTaggedAndIsEnabledTrue()
    {
        await InitializeTagPeopleToolModel();
        tagPeopleToolModel.IsEnabled = true;

        messenger.Send(new SetTagPeopleToolActiveMessage(true));
        await tagPeopleToolModel.LastDispatchTask;

        AssertSuggestedFaces(exampleSuggestedFaces.Skip(1), tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(exampleSuggestedFaces[0], tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task ActivationMessage_DoesNotShowSuggestedFaces_WhenNoPeopleTaggedAndIsEnabledFalse()
    {
        await InitializeTagPeopleToolModel();
        tagPeopleToolModel.IsEnabled = false;

        messenger.Send(new SetTagPeopleToolActiveMessage(true));
        await tagPeopleToolModel.LastDispatchTask;

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task ActivationMessage_DoesNotShowSuggestedFaces_WhenPeopleTaggedAndIsEnabledTrue()
    {
        await InitializeTagPeopleToolModel(peopleTags: examplePeopleTags);

        messenger.Send(new SetTagPeopleToolActiveMessage(true));

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task ActivationMessage_DoesNotShowSuggestedFaces_WhenPeopleTaggedAndIsEnabledFalse()
    {
        await InitializeTagPeopleToolModel(peopleTags: examplePeopleTags);

        messenger.Send(new SetTagPeopleToolActiveMessage(true));

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task SetIsEnabledTrue_ShowsSuggestedFaces_WhenNoPeopleTaggedAndIsActiveTrue()
    {
        await InitializeTagPeopleToolModel(active: true);

        tagPeopleToolModel.IsEnabled = true;

        AssertSuggestedFaces(exampleSuggestedFaces.Skip(1), tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(exampleSuggestedFaces[0], tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task SetIsEnabledTrue_DoesNotSuggestedFaces_WhenNoPeopleTaggedAndIsActiveFalse()
    {
        await InitializeTagPeopleToolModel(active: false);

        tagPeopleToolModel.IsEnabled = true;

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task SetIsEnabledTrue_DoesNotShowSuggestedFaces_WhenPeopleTaggedAndIsActiveTrue()
    {
        await InitializeTagPeopleToolModel(active: true, peopleTags: examplePeopleTags);

        tagPeopleToolModel.IsEnabled = true;

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task SetIsEnabledTrue_DoesNotShowSuggestedFaces_WhenPeopleTaggedAndIsActiveFalse()
    {
        await InitializeTagPeopleToolModel(active: false);

        tagPeopleToolModel.IsEnabled = true;

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task SetIsEnabledFalse_ClearsSelectionRectAndAutoSuggestBoxTextAndSuggestedFaces()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.IsEnabled = false;

        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
        Assert.Equal(string.Empty, tagPeopleToolModel.AutoSuggestBoxText);
        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
    }

    [Fact]
    public async Task SetIsEnabledTrue_RestoresSuggestedFaces()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";
        tagPeopleToolModel.TrySelectNextDetectedFace();
        tagPeopleToolModel.IsEnabled = false;

        tagPeopleToolModel.IsEnabled = true;

        AssertSuggestedFaces(exampleSuggestedFaces.Skip(1), tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(exampleSuggestedFaces[0], tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task SetIsEnabledTrue_DoesNotRestoreSuggestedFaces_WhenPeopleTagged()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(examplePeopleTags);
        messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.People));
        await tagPeopleToolModel.LastDispatchTask;
        tagPeopleToolModel.IsEnabled = false;

        tagPeopleToolModel.IsEnabled = true;

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public void ExitPeopleTagging_SendsTagPeopleToolActiveChangedMessage()
    {
        var messageCapture = TestUtils.CaptureMessage<SetTagPeopleToolActiveMessage>(messenger);

        tagPeopleToolModel.ExitPeopleTagging();

        Assert.False(messageCapture.Message.IsActive);
    }

    [Fact]
    public async Task TrySelectNextDetectedFace_ShowsNextSuggestedFace()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.TrySelectNextDetectedFace();

        AssertSuggestedFaces(exampleSuggestedFaces.Skip(2), tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(exampleSuggestedFaces[1], tagPeopleToolModel.SelectionRectInPercent);

        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.TrySelectNextDetectedFace();

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(exampleSuggestedFaces[2], tagPeopleToolModel.SelectionRectInPercent);

        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.TrySelectNextDetectedFace();

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(Rect.Empty, tagPeopleToolModel.SelectionRectInPercent);
        Assert.Equal(string.Empty, tagPeopleToolModel.AutoSuggestBoxText);
    }

    [Fact]
    public async Task OnUserStartedSelection_SetsIsNameInputVisibleFalseAndClearsSuggestedFaces()
    {
        await InitializeTagPeopleToolModel(active: true, enbaled: true);

        tagPeopleToolModel.OnUserStartedSelection();

        Assert.False(tagPeopleToolModel.IsNameInputVisible);
        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
    }

    [Fact]
    public async Task OnUserEndedSelection_SetsIsNameInputVisibleTrue()
    {
        await InitializeTagPeopleToolModel(active: true, enbaled: true);
        tagPeopleToolModel.SelectionRectInPercent = new Rect(0.437, 0.254, 0.098, 0.102);

        tagPeopleToolModel.OnUserEndedSelection();

        Assert.True(tagPeopleToolModel.IsNameInputVisible);
    }

    [Fact]
    public async Task AddPersonCommandExecuteAsync_AddsPeopleTagToFileAndShowsNextSelectedFace()
    {
        await InitializeTagPeopleToolModel(active: true);
        var peopleTags = new List<PeopleTag>() { new PeopleTag("Already Tagged") };
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(peopleTags);
        var metadataModifiedMessageCapture = TestUtils.CaptureMessage<MetadataModifiedMessage>(messenger);
        tagPeopleToolModel.IsEnabled = true;

        tagPeopleToolModel.AutoSuggestBoxText = "New Person";
        await tagPeopleToolModel.AddPersonCommand.ExecuteAsync(null);

        await metadataService.Received().WriteMetadataAsync(bitmapFile, MetadataProperties.People,
            Arg.Is<IList<PeopleTag>>(peopleTags => peopleTags[1].Name == "New Person"));
        Assert.Single(metadataModifiedMessageCapture.Message.Files);
        Assert.Equal(bitmapFile, metadataModifiedMessageCapture.Message.Files.First());
        Assert.Equal(MetadataProperties.People, metadataModifiedMessageCapture.Message.MetadataProperty);
        await suggestionsService.Received().AddSuggestionAsync("New Person");
        Assert.Single(tagPeopleToolModel.TaggedPeople);
        Assert.True(tagPeopleToolModel.TaggedPeople[0].IsVisible);
        AssertSuggestedFaces(exampleSuggestedFaces.Skip(2), tagPeopleToolModel.SuggestedFaces);
        Assert.Equal(exampleSuggestedFaces[1], tagPeopleToolModel.SelectionRectInPercent);
    }

    [Fact]
    public async Task AddPersonCommandExecuteAsync_ShowsErrorDialog_WhenPersonAlreadyTagged()
    {
        await InitializeTagPeopleToolModel(active: true);
        var peopleTags = new List<PeopleTag>() { new PeopleTag("Already Tagged") };
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(peopleTags);
        tagPeopleToolModel.IsEnabled = true;

        tagPeopleToolModel.AutoSuggestBoxText = "Already Tagged";
        await tagPeopleToolModel.AddPersonCommand.ExecuteAsync(null);

        await dialogService.Received().ShowDialogAsync(Arg.Any<MessageDialogModel>());
    }

    [Fact]
    public async Task AddPersonCommandExecuteAsync_DoesNothing_WhenNoNameEntered()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;

        await tagPeopleToolModel.AddPersonCommand.ExecuteAsync(null);

        await metadataService.DidNotReceive().WriteMetadataAsync(bitmapFile, MetadataProperties.People, Arg.Any<IList<PeopleTag>>());
    }

    private async Task InitializeTagPeopleToolModel(bool active = false, bool enbaled = false, List<PeopleTag>? peopleTags = null)
    {
        messenger.Register<IsTagPeopleToolActiveRequestMessage>(this, (_, msg) => msg.Reply(active));
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(peopleTags ?? new List<PeopleTag>());
        await tagPeopleToolModel.InitializeAsync();
        tagPeopleToolModel.BitmapImage = exampleBitmapImage;
        await tagPeopleToolModel.ProcessBitmapImageTask;
        tagPeopleToolModel.IsEnabled = enbaled;
    }

    private void AssertSuggestedFaces(IEnumerable<Rect> expectedSuggestedFaces, IObservableReadOnlyList<Rect> suggestedFaces)
    {
        Assert.Equal(expectedSuggestedFaces.Count(), suggestedFaces.Count);
        int i = 0;
        foreach (var expectedRect in expectedSuggestedFaces)
        {
            Assert.Equal(expectedRect, suggestedFaces[i++]);
        }
    }

}
