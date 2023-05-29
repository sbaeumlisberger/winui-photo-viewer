using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public class MathUtil
{
    public static double Diff(double a, double b)
    {
        return Math.Abs(a - b);
    }

    public static bool ApproximateEquals(double valueA, double valueB, double delta = 0.001)
    {
        return Diff(valueA, valueB) < delta;
    }
}
