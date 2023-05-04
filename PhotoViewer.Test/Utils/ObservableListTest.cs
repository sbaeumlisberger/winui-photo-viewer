using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PhotoViewer.Test.Utils;

public class ObservableListTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public ObservableListTest(ITestOutputHelper testOutputHelper) 
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void MatchTo_MovesExistingElements()
    {
        int numberOfMoves = 0;
        var observableList = new ObservableList<int>(new[] { 1, 2, 3, 4 });
        observableList.CollectionChanged += ObservableList_CollectionChanged;
        void ObservableList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                numberOfMoves++;
            }
        }
        var otherCollection = new[] { 2, 4, 3, 1 };

        observableList.MatchTo(otherCollection);

        Assert.Equal(otherCollection, observableList);
        Assert.Equal(3, numberOfMoves);
    }

    [Fact]
    public void MatchTo_AddsNewElements()
    {
        int numberOfAdds = 0;
        var observableList = new ObservableList<int>(new[] { 1, 2, 3, 4 });
        observableList.CollectionChanged += ObservableList_CollectionChanged;
        void ObservableList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                numberOfAdds++;
            }
        }
        var otherCollection = new[] { 1, 10, 2, 3, 30, 4, 40 };

        observableList.MatchTo(otherCollection);

        Assert.Equal(otherCollection, observableList);
        Assert.Equal(3, numberOfAdds);
    }

    [Fact]
    public void MatchTo_ReplacesElements()
    {
        int numberOfReplaces = 0;
        var observableList = new ObservableList<int>(new[] { 1, 2, 3, 4 });
        observableList.CollectionChanged += ObservableList_CollectionChanged;
        void ObservableList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                numberOfReplaces++;
            }
        }
        var otherCollection = new[] { 1, 20, 3, 40 };

        observableList.MatchTo(otherCollection);

        Assert.Equal(otherCollection, observableList);
        Assert.Equal(2, numberOfReplaces);
    }

    [Fact]
    public void MatchTo_RemovesElements()
    {
        int numberOfRemoves = 0;
        var observableList = new ObservableList<int>(new[] { 1, 2, 3, 4 });
        observableList.CollectionChanged += ObservableList_CollectionChanged;
        void ObservableList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                numberOfRemoves++;
            }
        }
        var otherCollection = new[] { 1, 2 };

        observableList.MatchTo(otherCollection);

        Assert.Equal(otherCollection, observableList);
        Assert.Equal(2, numberOfRemoves);
    }

    [Fact]
    public void MatchTo_CombinedTest()
    {
        int numberOfMoves = 0;
        int numberOfAdds = 0;
        int numberOfReplaces = 0;
        int numberOfRemoves = 0;
        var observableList = new ObservableList<int>(new[] { 1, 2, 3, 4, 5, 6 });
        observableList.CollectionChanged += ObservableList_CollectionChanged;
        void ObservableList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            testOutputHelper.WriteLine("Action: " + e.Action);
            testOutputHelper.WriteLine(string.Join(", ", observableList));
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                numberOfMoves++;
            }
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                numberOfAdds++;
            }
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                numberOfReplaces++;
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                numberOfRemoves++;
            }
        }
        var otherCollection = new[] { 5, 4, 10, 20, 2 };

        observableList.MatchTo(otherCollection);

        Assert.Equal(otherCollection, observableList);
        Assert.Equal(2, numberOfMoves);
        Assert.Equal(1, numberOfAdds);
        Assert.Equal(1, numberOfReplaces);
        Assert.Equal(2, numberOfRemoves);
    }
}
