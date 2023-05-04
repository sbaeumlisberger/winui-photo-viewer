using PhotoViewer.App.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Utils;

public class CollectionsUtilTest
{

    [Fact]
    public void GetNeighbours()
    {
        var list = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        Assert.Equal(new[] { 1 }, list.GetNeighbours(0));
        Assert.Equal(new[] { 0, 2 }, list.GetNeighbours(1));
        Assert.Equal(new[] { 7, 9 }, list.GetNeighbours(8));
        Assert.Equal(new[] { 8 }, list.GetNeighbours(9));
        Assert.Equal(new[] { 3, 4, 6, 7 }, list.GetNeighbours(5, 2));
    }

}
