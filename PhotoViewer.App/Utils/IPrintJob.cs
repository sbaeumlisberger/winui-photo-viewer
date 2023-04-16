using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;

namespace PhotoViewer.App.Utils;

internal interface IPrintJob
{
    string Title { get; }

    void SetupPrintOptions(PrintTaskOptionDetails optionDetails);

    void UpdateDisplayedOptions(PrintTaskOptionDetails optionDetails);

    List<UIElement> CreatePages(PrintTaskOptions options, bool isPreview, Action invalidatePreviewCallback);
}
