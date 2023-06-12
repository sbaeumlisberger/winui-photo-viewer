using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class KeywordSectionModelTest
{
    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly ISuggestionsService suggestionsService = Substitute.For<ISuggestionsService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly KeywordsSectionModel keywordsSectionModel;

    public KeywordSectionModelTest()
    {
        keywordsSectionModel = new KeywordsSectionModel(messenger, metadataService, suggestionsService, dialogService, backgroundTaskService);
    }

    [Fact]
    public void UpdatesKeywordsList_WhenFilesChanged()
    {
        var files = Substitute.For<IReadOnlyList<IBitmapFileInfo>>();
        var metadata = new[]
        {
            CreateMetadataView("Category 1/Keyword 1", "Category 1/Keyword 2"),
            CreateMetadataView("Category 1/Keyword 2"),
            CreateMetadataView("Category 1/Keyword 1", "Category 2/Keyword 3"),
            CreateMetadataView("Category 1/Keyword 2")
        };

        keywordsSectionModel.UpdateFilesChanged(files, metadata);

        var keywordItems = keywordsSectionModel.Keywords;

        Assert.Equal(3, keywordItems.Count);

        Assert.Equal("Category 1/Keyword 1", keywordItems[0].Value);
        Assert.Equal("Keyword 1", keywordItems[0].ShortValue);
        Assert.Equal(2, keywordItems[0].Count);
        Assert.Equal(4, keywordItems[0].Total);

        Assert.Equal("Category 1/Keyword 2", keywordItems[1].Value);
        Assert.Equal("Keyword 2", keywordItems[1].ShortValue);
        Assert.Equal(3, keywordItems[1].Count);
        Assert.Equal(4, keywordItems[1].Total);

        Assert.Equal("Category 2/Keyword 3", keywordItems[2].Value);
        Assert.Equal("Keyword 3", keywordItems[2].ShortValue);
        Assert.Equal(1, keywordItems[2].Count);
        Assert.Equal(4, keywordItems[2].Total);
    }

    [Fact]
    public void UpdatesKeywordsList_WhenMetadataModified()
    {
        var files = Substitute.For<IReadOnlyList<IBitmapFileInfo>>();
        var metadataFile1 = CreateMetadataView("Category 1/Keyword 1");
        var metadataFile2 = CreateMetadataView("Category 1/Keyword 2");
        var metadata = new[] { metadataFile1, metadataFile2 };
        keywordsSectionModel.UpdateFilesChanged(files, metadata);

        UpdateMetadataView(metadataFile1, "Category 1/Keyword 1", "Category New/Keyword New");
        keywordsSectionModel.UpdateMetadataModified(MetadataProperties.Keywords);

        var keywordItems = keywordsSectionModel.Keywords;

        Assert.Equal(3, keywordItems.Count);

        Assert.Equal("Category New/Keyword New", keywordItems[1].Value);
        Assert.Equal("Keyword New", keywordItems[1].ShortValue);
        Assert.Equal(1, keywordItems[1].Count);
        Assert.Equal(2, keywordItems[1].Total);
    }

    [Fact]
    public void AddKeywordCommandCanNotExecute_WhenAutoSuggestBoxTextEmtpty()
    {
        Assert.False(keywordsSectionModel.AddKeywordCommand.CanExecute(null));
    }

    [Fact]
    public void AddKeywordCommandCanNotExecute_WhenAutoSuggestBoxTextIsWhitespace()
    {
        keywordsSectionModel.AutoSuggestBoxText = "   ";
        Assert.False(keywordsSectionModel.AddKeywordCommand.CanExecute(null));
    }

    [Fact]
    public void AddKeywordCommandCanNotExecute_WhenIsWriting()
    {
        var file = MockBitmapFileInfo();
        keywordsSectionModel.UpdateFilesChanged(new[] { file }, Substitute.For<IList<MetadataView>>());
        keywordsSectionModel.AutoSuggestBoxText = "test";
        var tsc = new TaskCompletionSource();
        metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, Arg.Any<string[]>()).Returns(tsc.Task);
        bool canExecuteChanged = false;
        keywordsSectionModel.AddKeywordCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        keywordsSectionModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.False(keywordsSectionModel.AddKeywordCommand.CanExecute(null));
        Assert.True(canExecuteChanged);
       
        tsc.SetResult();
    }

    [Fact]
    public async Task AddKeywordCommandAddsKeywordToFiles()
    {
        var files = new[]
        {
            MockBitmapFileInfo(),
            MockBitmapFileInfo("Category A/Keyword 1"),
            MockBitmapFileInfo("Category B/Keyword 2"),
            MockBitmapFileInfo("Category C/Keyword 2")
        };
        keywordsSectionModel.UpdateFilesChanged(files, Substitute.For<IList<MetadataView>>());
        string keyword = "Category B/Keyword 2";
        keywordsSectionModel.AutoSuggestBoxText = keyword;
        var messageCapture = TestUtils.CaptureMessage<MetadataModifiedMessage>(messenger);

        await keywordsSectionModel.AddKeywordCommand.ExecuteAsync(null);

        await VerifyReceivedWriteMetadataAsync(files[0], "Category B/Keyword 2");
        await VerifyReceivedWriteMetadataAsync(files[1], "Category A/Keyword 1", "Category B/Keyword 2");
        await metadataService.DidNotReceive().WriteMetadataAsync(files[2], MetadataProperties.Keywords, Arg.Any<string[]>());
        await VerifyReceivedWriteMetadataAsync(files[3], "Category C/Keyword 2", "Category B/Keyword 2");
        Assert.Empty(keywordsSectionModel.AutoSuggestBoxText);
        await suggestionsService.Received().AddSuggestionAsync(keyword);
        Assert.NotNull(messageCapture.Message);
        Assert.Equal(3, messageCapture.Message.Files.Count);
        Assert.Equal(MetadataProperties.Keywords, messageCapture.Message.MetadataProperty);
    }

    [Fact]
    public async Task RemoveKeywordCommandRemovesKeywordFromFiles()
    {
        var files = new[]
       {
            MockBitmapFileInfo(),
            MockBitmapFileInfo("Category A/Keyword 1", "Category B/Keyword 2", "Category C/Keyword 1"),
            MockBitmapFileInfo("Category B/Keyword 2"),
            MockBitmapFileInfo("Category C/Keyword 2", "Category B/Keyword 2")
        };
        keywordsSectionModel.UpdateFilesChanged(files, Substitute.For<IList<MetadataView>>());
        string keyword = "Category B/Keyword 2";
        var messageCapture = TestUtils.CaptureMessage<MetadataModifiedMessage>(messenger);

        await keywordsSectionModel.RemoveKeywordCommand.ExecuteAsync(keyword);

        await metadataService.DidNotReceive().WriteMetadataAsync(files[0], MetadataProperties.Keywords, Arg.Any<string[]>());
        await VerifyReceivedWriteMetadataAsync(files[1], "Category A/Keyword 1", "Category C/Keyword 1");
        await VerifyReceivedWriteMetadataAsync(files[2], new string[0]);
        await VerifyReceivedWriteMetadataAsync(files[3], "Category C/Keyword 2");     
        Assert.NotNull(messageCapture.Message);
        Assert.Equal(3, messageCapture.Message.Files.Count);
        Assert.Equal(MetadataProperties.Keywords, messageCapture.Message.MetadataProperty);
    }

    private MetadataView CreateMetadataView(params string[] keywords)
    {
        return new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.Keywords.Identifier, keywords }
        });
    }

    private void UpdateMetadataView(MetadataView metadataView, params string[] keywords)
    {
        metadataView.Source[MetadataProperties.Keywords.Identifier] = keywords;
    }

    private IBitmapFileInfo MockBitmapFileInfo(params string[] keywords)
    {
        var file = Substitute.For<IBitmapFileInfo>();
        metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).Returns(keywords);
        return file;
    }

    private async Task VerifyReceivedWriteMetadataAsync(IBitmapFileInfo file, params string[] keywords)
    {
        await metadataService.Received().WriteMetadataAsync(
            file,
            MetadataProperties.Keywords,
            Arg.Is<string[]>(arg => arg.SequenceEqual(keywords)));
    }


}
