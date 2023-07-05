using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
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

    private readonly List<Rect> expectedSuggestedFaces;

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

        expectedSuggestedFaces = new List<Rect>() {
            new Rect(0.23, 0.22, 0.19, 0.42),
            new Rect(0.505, 0.23, 0.185, 0.41),
            new Rect(0.76, 0.19, 0.18, 0.4),
        };

        faceDetectionService.DetectFacesAsync(exampleBitmapImage, Arg.Any<CancellationToken>()).Returns(exampleDetectedFaces);

        tagPeopleToolModel = new TagPeopleToolModel(bitmapFile, messenger, suggestionsService, metadataService, dialogService, faceDetectionService);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LodasPeopleTags_OnInitialization(bool isActive)
    {
        messenger.Register<IsTagPeopleToolActiveRequestMessage>(this, (_, msg) => msg.Reply(isActive));
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(examplePeopleTags);

        await tagPeopleToolModel.InitializeAsync();

        Assert.Equal(isActive, tagPeopleToolModel.IsActive);
        Assert.Equal(2, tagPeopleToolModel.TaggedPeople.Count);

        var peopleTagVM1 = tagPeopleToolModel.TaggedPeople[0];
        Assert.Equal(examplePeopleTag1.Name, peopleTagVM1.Name);
        Assert.Equal(isActive, peopleTagVM1.IsVisible);
        Assert.Equal(examplePeopleTag1.Rectangle.ToRect(), peopleTagVM1.FaceBox);

        var peopleTagVM2 = tagPeopleToolModel.TaggedPeople[1];
        Assert.Equal(examplePeopleTag3.Name, peopleTagVM2.Name);
        Assert.Equal(isActive, peopleTagVM2.IsVisible);
        Assert.Equal(examplePeopleTag3.Rectangle.ToRect(), peopleTagVM2.FaceBox);
    }

    [Fact]
    public async void ShowsTaggedPeople_OnActivation()
    {
        await InitializeTagPeopleToolModel(peopleTags: examplePeopleTags);

        messenger.Send(new SetTagPeopleToolActiveMessage(true));

        Assert.All(tagPeopleToolModel.TaggedPeople, peopleTagVM => Assert.True(peopleTagVM.IsVisible));
        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuggestsFaces_OnActivation_WhenNoPeopleTagged(bool isEnabled)
    {
        await InitializeTagPeopleToolModel();
        tagPeopleToolModel.IsEnabled = isEnabled;

        messenger.Send(new SetTagPeopleToolActiveMessage(true));
        await tagPeopleToolModel.LastDispatchTask;

        if (isEnabled)
        {
            AssertSuggestedFaces(tagPeopleToolModel.SuggestedFaces, 1);
            Assert.Equal(new Rect(0.23, 0.22, 0.19, 0.42), tagPeopleToolModel.SelectionRect);
        }
        else
        {
            AssertSuggestedFaces(tagPeopleToolModel.SuggestedFaces, 0);
        }
    }

    [Fact]
    public async Task SuggestsNoFaces_OnActivation_WhenPeopleTagged()
    {
        await InitializeTagPeopleToolModel(peopleTags: examplePeopleTags);

        messenger.Send(new SetTagPeopleToolActiveMessage(true));

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
    }

    [Fact]
    public async Task SelectsFirstSuggestFace_OnEnabled()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.BitmapImage = exampleBitmapImage;

        tagPeopleToolModel.IsEnabled = true;

        AssertSuggestedFaces(tagPeopleToolModel.SuggestedFaces, 1);
        Assert.Equal(expectedSuggestedFaces[0], tagPeopleToolModel.SelectionRect);
    }

    [Fact]
    public async Task SelectsFirstSuggestFace_OnEnabledAfterDisabled()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.BitmapImage = exampleBitmapImage;

        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.IsEnabled = false;
        tagPeopleToolModel.IsEnabled = true;

        AssertSuggestedFaces(tagPeopleToolModel.SuggestedFaces, 1);
        Assert.Equal(expectedSuggestedFaces[0], tagPeopleToolModel.SelectionRect);
    }

    [Fact]
    public async Task ClearsSelectionRectAndAutoSuggestBoxText_OnDisabled()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.IsEnabled = false;
        
        Assert.Equal(default, tagPeopleToolModel.SelectionRect);
        Assert.Equal(string.Empty, tagPeopleToolModel.AutoSuggestBoxText);
        Assert.Equal(expectedSuggestedFaces.Count, tagPeopleToolModel.SuggestedFaces.Count);
    }

    [Fact]
    public async Task RestoresSuggestedFaces_OnDisabled()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";
        tagPeopleToolModel.SkipCurrentDetectedFace();

        tagPeopleToolModel.IsEnabled = false;

        Assert.Equal(expectedSuggestedFaces.Count, tagPeopleToolModel.SuggestedFaces.Count);
    }

    [Fact]
    public async Task ClearsSuggestedFaces_OnDisabledWhenPeopleTagged()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(examplePeopleTags);
        messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.People));
        await tagPeopleToolModel.LastDispatchTask;

        tagPeopleToolModel.IsEnabled = false;

        Assert.Empty(tagPeopleToolModel.SuggestedFaces);
    }

    [Fact]
    public void SendsTagPeopleToolActiveChangedMessage_WhenExitPeopleTaggingCalled()
    {
        var messageCapture = TestUtils.CaptureMessage<SetTagPeopleToolActiveMessage>(messenger);

        tagPeopleToolModel.ExitPeopleTagging();

        Assert.False(messageCapture.Message.IsActive);
    }

    [Fact]
    public async Task ShowsNextFace_WhenSkipCurrentDetectedFaceCalled()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;
        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.SkipCurrentDetectedFace();

        AssertSuggestedFaces(tagPeopleToolModel.SuggestedFaces, 2);
        Assert.Equal(expectedSuggestedFaces[1], tagPeopleToolModel.SelectionRect);

        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.SkipCurrentDetectedFace();

        Assert.Equal(0, tagPeopleToolModel.SuggestedFaces.Count);
        Assert.Equal(expectedSuggestedFaces[2], tagPeopleToolModel.SelectionRect);

        tagPeopleToolModel.AutoSuggestBoxText = "Some Name";

        tagPeopleToolModel.SkipCurrentDetectedFace();

        Assert.Equal(0, tagPeopleToolModel.SuggestedFaces.Count);
        Assert.Equal(default, tagPeopleToolModel.SelectionRect);
        Assert.Equal(string.Empty, tagPeopleToolModel.AutoSuggestBoxText);
    }

    [Fact]
    public async Task ShowsErrorDialog_WhenAlreadyTaggedPersonAdded()
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
    public async Task DoesNothing_WhenPersonAddedWithoutName()
    {
        await InitializeTagPeopleToolModel(active: true);
        tagPeopleToolModel.IsEnabled = true;

        await tagPeopleToolModel.AddPersonCommand.ExecuteAsync(null);

        await metadataService.DidNotReceive().WriteMetadataAsync(bitmapFile, MetadataProperties.People, Arg.Any<IList<PeopleTag>>());
    }

    [Fact]
    public async Task AddsPeopleTagToFileAndShowsNextSelectedFace_WhenPersonAdded()
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
        AssertSuggestedFaces(tagPeopleToolModel.SuggestedFaces, 2);
        Assert.Equal(expectedSuggestedFaces[1], tagPeopleToolModel.SelectionRect);
    }

    private async Task InitializeTagPeopleToolModel(bool active = false, List<PeopleTag>? peopleTags = null)
    {
        messenger.Register<IsTagPeopleToolActiveRequestMessage>(this, (_, msg) => msg.Reply(active));
        metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People).Returns(peopleTags ?? new List<PeopleTag>());
        await tagPeopleToolModel.InitializeAsync();
        tagPeopleToolModel.BitmapImage = exampleBitmapImage;
    }

    private void AssertSuggestedFaces(IObservableReadOnlyList<Rect> suggestedFaces, int skip = 0)
    {
        Assert.Equal(expectedSuggestedFaces.Count - skip, suggestedFaces.Count);
        int i = 0;
        foreach (var expectedRect in expectedSuggestedFaces.Skip(skip))
        {
            Assert.Equal(expectedRect, suggestedFaces[i++]);
        }
    }

}
