using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;
internal class MathUtil
{
    public static double Diff(double a, double b) 
    {
        return Math.Abs(a - b);
    }

}
