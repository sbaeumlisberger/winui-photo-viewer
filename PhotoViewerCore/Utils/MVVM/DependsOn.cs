using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.Utils;

public class DependsOnAttribute : Attribute
{
    public DependsOnAttribute(params string[] properties)
    {
    }
}
