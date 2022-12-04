using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Models;
using PhotoViewerApp.Resources;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;


namespace PhotoViewerApp.Views;

public sealed partial class DetailsBar : UserControl
{

    private DetailsBarModel ViewModel => (DetailsBarModel)DataContext;

    public DetailsBar()
    {
        this.InitializeMVVM<DetailsBarModel>(InitializeComponent);
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
