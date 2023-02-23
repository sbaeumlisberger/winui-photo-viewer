using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhotoViewer.App.Controls;

public class WrapGrid : Grid
{

    protected override Size MeasureOverride(Size availableSize)
    {
        var columnsCount = ColumnDefinitions.Count;

        RowDefinitions.Clear();
        RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

        int column = 0;
        int row = 0;
        foreach(FrameworkElement child in Children) 
        {
            SetColumn(child, column);
            SetRow(child, row);
            column += GetColumnSpan(child);
            if (column >= columnsCount) 
            {
                column = 0;
                row++;
                RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            }
        }            

        return base.MeasureOverride(availableSize);
    }
}
