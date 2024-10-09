using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class DetailsBar : UserControl, IMVVMControl<DetailsBarModel>
{
    public DetailsBar()
    {
        this.InitializeComponentMVVM();
    }

    private string ColorSpaceTypeToDisplayName(ColorSpaceType colorSpaceType)
    {
        switch (colorSpaceType)
        {
            case ColorSpaceType.SRGB:
                return Strings.DetailsBar_ColorSpaceSRGB;
            case ColorSpaceType.AdobeRGB:
                return Strings.DetailsBar_ColorSpaceAdobeRGB;
            default:
                return Strings.DetailsBar_ColorSpaceUnknown;
        }
    }
}
