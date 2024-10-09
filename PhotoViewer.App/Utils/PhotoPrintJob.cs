using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.Storage;
using Windows.UI;

namespace PhotoViewer.App.Utils;

internal class PhotoPrintJob : IPrintJob
{
    private static class PhotoSizeOption
    {
        public const string OptionId = nameof(PhotoSizeOption);
        public const string SizeFullPage = nameof(SizeFullPage);
        public const string Size130x180 = nameof(Size130x180);
        public const string Size100x150 = nameof(Size100x150);
        public const string Size90x130 = nameof(Size90x130);
        public const string Size55x85 = nameof(Size55x85);
        public const string Grid = nameof(Grid);
        public const string Custom = nameof(Custom);
    }

    private static class CopiesOption
    {
        public const string OptionId = nameof(CopiesOption);
    }

    private static class PhotoSizeWidthOption
    {
        public const string OptionId = nameof(PhotoSizeWidthOption);
    }

    private static class PhotoSizeHeightOption
    {
        public const string OptionId = nameof(PhotoSizeHeightOption);
    }

    private static class PhotoSizeColumnsOption
    {
        public const string OptionId = nameof(PhotoSizeColumnsOption);
    }

    private static class PhotoSizeRowsOption
    {
        public const string OptionId = nameof(PhotoSizeRowsOption);
    }
    private static class CollapseUnusedSpaceOption
    {
        public const string OptionId = nameof(CollapseUnusedSpaceOption);
    }

    private static class HorizontalAlignmentOption
    {
        public const string OptionId = nameof(HorizontalAlignmentOption);
        public const string Left = nameof(Left);
        public const string Center = nameof(Center);
        public const string Right = nameof(Right);
    }

    /// <summary>conversion factor from cm (centimeters) to inches</summary>
    private const double CmToInch = 0.393701;

    /// <summary>conversion factor from cm (centimeters) to DIP (device independent pixels)</summary>
    private const double CmToDIP = CmToInch * 96;

    private static readonly Color ImageBoxBackgroundColor = Color.FromArgb(255, 240, 240, 240);

    public string Title => string.Join(", ", files.Select(file => file.FileName));

    private readonly IList<IMediaFileInfo> files;
    private ImageSource[]? images;

    public PhotoPrintJob(IList<IMediaFileInfo> files)
    {
        this.files = files;
    }

    public void SetupPrintOptions(PrintTaskOptionDetails optionDetails)
    {
        var copiesOption = optionDetails.CreateTextOption(CopiesOption.OptionId, Strings.Printing_CopiesTitle);
        copiesOption.TrySetValue("1");

        var photoSizeOption = optionDetails.CreateItemListOption(PhotoSizeOption.OptionId, Strings.Printing_LayoutTitle);
        photoSizeOption.AddItem(PhotoSizeOption.SizeFullPage, Strings.Printing_LayoutFullPage);
        photoSizeOption.AddItem(PhotoSizeOption.Size130x180, Strings.Printing_Layout130x180);
        photoSizeOption.AddItem(PhotoSizeOption.Size100x150, Strings.Printing_Layout100x150);
        photoSizeOption.AddItem(PhotoSizeOption.Size90x130, Strings.Printing_Layout90x130);
        photoSizeOption.AddItem(PhotoSizeOption.Size55x85, Strings.Printing_Layout55x85);
        photoSizeOption.AddItem(PhotoSizeOption.Grid, Strings.Printing_LayoutGrid);
        photoSizeOption.AddItem(PhotoSizeOption.Custom, Strings.Printing_LayoutCustom);

        optionDetails.CreateTextOption(PhotoSizeColumnsOption.OptionId, Strings.Printing_GridColumnsTitle).TrySetValue("2");
        optionDetails.CreateTextOption(PhotoSizeRowsOption.OptionId, Strings.Printing_GridRowsTitle).TrySetValue("2");

        optionDetails.CreateTextOption(PhotoSizeWidthOption.OptionId, Strings.Printing_CustomWidthTitle).TrySetValue("15");
        optionDetails.CreateTextOption(PhotoSizeHeightOption.OptionId, Strings.Printing_CustomHeightTitle).TrySetValue("10");

        var horizontalAlignmentOption = optionDetails.CreateItemListOption(HorizontalAlignmentOption.OptionId, Strings.Printing_HorizontalAlignmentTitle);
        horizontalAlignmentOption.AddItem(HorizontalAlignmentOption.Left, Strings.Printing_HorizontalAlignmentLeft);
        horizontalAlignmentOption.AddItem(HorizontalAlignmentOption.Center, Strings.Printing_HorizontalAlignmentCenter);
        horizontalAlignmentOption.AddItem(HorizontalAlignmentOption.Right, Strings.Printing_HorizontalAlignmentRight);

        optionDetails.CreateToggleOption(CollapseUnusedSpaceOption.OptionId, Strings.Printing_CollapseUnusedSpaceTitle);
    }

    public void UpdateDisplayedOptions(PrintTaskOptionDetails optionDetails)
    {
        var displayedOptions = new List<string>();

        displayedOptions.Add(StandardPrintTaskOptions.Orientation);
        displayedOptions.Add(CopiesOption.OptionId);

        displayedOptions.Add(PhotoSizeOption.OptionId);

        if (optionDetails.Options.TryGetValue(PhotoSizeOption.OptionId, out IPrintOptionDetails? photoSizeOption))
        {
            string photoSizeOptionValue = (string)photoSizeOption.Value;

            if (photoSizeOptionValue == PhotoSizeOption.Custom)
            {
                displayedOptions.Add(PhotoSizeWidthOption.OptionId);
                displayedOptions.Add(PhotoSizeHeightOption.OptionId);
            }
            else if (photoSizeOptionValue == PhotoSizeOption.Grid)
            {
                displayedOptions.Add(PhotoSizeColumnsOption.OptionId);
                displayedOptions.Add(PhotoSizeRowsOption.OptionId);
            }

            if (photoSizeOptionValue != PhotoSizeOption.SizeFullPage)
            {
                displayedOptions.Add(HorizontalAlignmentOption.OptionId);
                displayedOptions.Add(CollapseUnusedSpaceOption.OptionId);
            }
        }

        displayedOptions.Add(StandardPrintTaskOptions.MediaType);
        displayedOptions.Add(StandardPrintTaskOptions.PrintQuality);

        optionDetails.DisplayedOptions.Clear();
        optionDetails.DisplayedOptions.AddRange(displayedOptions);
    }

    public List<UIElement> CreatePages(PrintTaskOptions options, bool isPreview, Action invalidatePreviewCallback)
    {
        if (images is null)
        {
            LoadImagesAsync(invalidatePreviewCallback);
            return new List<UIElement>();
        }

        var pageDescription = options.GetPageDescription(0);
        Size pageSize = pageDescription.PageSize;
        Rect printableArea = pageDescription.ImageableRect;

        var optionDetails = PrintTaskOptionDetails.GetFromPrintTaskOptions(options).Options;

        if (!uint.TryParse((string)optionDetails[CopiesOption.OptionId].Value, out uint copies))
        {
            optionDetails[CopiesOption.OptionId].ErrorText = Strings.Printing_CopiesInvalidError;
            return new List<UIElement>();
        }
        else
        {
            optionDetails[CopiesOption.OptionId].ErrorText = "";
        }

        string photoSizeItemId = (string)optionDetails[PhotoSizeOption.OptionId].Value;

        if (photoSizeItemId == PhotoSizeOption.SizeFullPage)
        {
            return LayoutFullPage(images, pageSize, printableArea, copies, isPreview);
        }
        else
        {
            Size photoSize = GetPhotoSize(photoSizeItemId, printableArea, optionDetails);
            return LayoutBasedOnSize(images, pageSize, printableArea, copies, isPreview, photoSize, optionDetails);
        }
    }

    private List<UIElement> LayoutFullPage(ImageSource[] images, Size pageSize, Rect printableArea, uint copies, bool isPreview)
    {
        var pages = new List<UIElement>();
        foreach (var img in images)
        {
            for (int i = 0; i < copies; i++)
            {
                ContentControl page = new ContentControl()
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };
                Grid imgBox = new Grid()
                {
                    Width = printableArea.Width,
                    Height = printableArea.Height,
                    Margin = new Thickness(printableArea.X, printableArea.Y, 0, 0),
                    Background = isPreview ? new SolidColorBrush(ImageBoxBackgroundColor) : null,
                    Children =
                    {
                        new Image()
                        {
                            Source = img,
                            Stretch = Stretch.Uniform
                        }
                    }
                };
                page.Content = imgBox;
                pages.Add(page);
            }
        }
        return pages;
    }

    private List<UIElement> LayoutBasedOnSize(ImageSource[] images, Size pageSize, Rect printableArea, uint copies, bool isPreview, Size photoSize, IReadOnlyDictionary<string, IPrintOptionDetails> options)
    {
        if (photoSize.Width == 0 || photoSize.Height == 0)
        {
            return new List<UIElement>();
        }

        int rows = (int)Math.Floor(printableArea.Height / photoSize.Height); // number of photos on y-axis
        int cols = (int)Math.Floor(printableArea.Width / photoSize.Width); // number of photos on x-axis

        if (rows == 0 || cols == 0)
        {
            options[PhotoSizeOption.OptionId].ErrorText = Strings.Printing_PhotoDoesNotFitError;
            return new List<UIElement>();
        }
        else
        {
            options[PhotoSizeOption.OptionId].ErrorText = "";
        }

        var horizontalAlignment = ToHorizontalAlignment(options[HorizontalAlignmentOption.OptionId]);
        var collapseUnusedSpace = (bool)options[CollapseUnusedSpaceOption.OptionId].Value;

        var pages = new List<UIElement>();
        int imgIndex = 0;
        for (int i = 0; i < copies * images.Length;)
        {
            ContentControl page = new ContentControl()
            {
                Width = pageSize.Width,
                Height = pageSize.Height
            };
            StackPanel content = new StackPanel()
            {
                Margin = new Thickness(printableArea.X, printableArea.Y, 0, 0),
                Width = printableArea.Width,
                Height = printableArea.Height
            };
            for (double y = 0; y < rows && i < copies * images.Length; y++)
            {
                StackPanel row = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = horizontalAlignment
                };
                for (double x = 0; x < cols && i < copies * images.Length; x++)
                {
                    Grid imgBox = new Grid()
                    {
                        Background = isPreview ? new SolidColorBrush(ImageBoxBackgroundColor) : null,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Children =
                        {
                            new Image()
                            {
                                Source = images[imgIndex],
                                Stretch = Stretch.Uniform,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    };
                    if (collapseUnusedSpace)
                    {
                        imgBox.MaxWidth = photoSize.Width;
                        imgBox.MaxHeight = photoSize.Height;
                    }
                    else
                    {
                        imgBox.Width = photoSize.Width;
                        imgBox.Height = photoSize.Height;
                    }
                    row.Children.Add(imgBox);
                    i++;
                    if (i % copies == 0)
                    {
                        imgIndex++;
                    }
                }
                content.Children.Add(row);
            }
            page.Content = content;
            pages.Add(page);
        }
        return pages;
    }

    private async void LoadImagesAsync(Action invalidatePreviewCallback)
    {
        try
        {
            images = await Task.WhenAll(files.Select(async file =>
            {
                var bitmapImage = new BitmapImage();
                using var stream = await file.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);
                await bitmapImage.SetSourceAsync(stream);
                return bitmapImage;
            }));
            invalidatePreviewCallback();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load images for printing", ex);
        }
    }

    private Size GetPhotoSize(string photoSizeItemId, Rect printableArea, IReadOnlyDictionary<string, IPrintOptionDetails> options)
    {
        switch (photoSizeItemId)
        {
            case PhotoSizeOption.Size130x180:
                return new Size(18 * CmToDIP, 13 * CmToDIP);
            case PhotoSizeOption.Size100x150:
                return new Size(15 * CmToDIP, 10 * CmToDIP);
            case PhotoSizeOption.Size90x130:
                return new Size(13 * CmToDIP, 9 * CmToDIP);
            case PhotoSizeOption.Size55x85:
                return new Size(8.5 * CmToDIP, 5.5 * CmToDIP);
            case PhotoSizeOption.Grid:
                return GetGridPhotoSize(printableArea, options);
            case PhotoSizeOption.Custom:
                return GetCustomPhotoSize(options);
            default:
                throw new UnreachableException();
        }
    }

    private Size GetCustomPhotoSize(IReadOnlyDictionary<string, IPrintOptionDetails> options)
    {
        double photoSizeWidth = 0;
        if (options.TryGetValue(PhotoSizeWidthOption.OptionId, out var widthOption))
        {
            bool isDouble = double.TryParse((string)widthOption.Value, out photoSizeWidth);
            if (isDouble && photoSizeWidth > 0)
            {
                widthOption.ErrorText = "";
            }
            else
            {
                widthOption.ErrorText = Strings.Printing_CustomSizeInvalidError;
                photoSizeWidth = 0;
            }
        }
        double photoSizeHeight = 0;
        if (options.TryGetValue(PhotoSizeHeightOption.OptionId, out var heightOption))
        {
            bool isDouble = double.TryParse((string)heightOption.Value, out photoSizeHeight);
            if (isDouble && photoSizeHeight > 0)
            {
                heightOption.ErrorText = "";
            }
            else
            {
                heightOption.ErrorText = Strings.Printing_CustomSizeInvalidError;
                photoSizeHeight = 0;
            }
        }
        return new Size(photoSizeWidth * CmToDIP, photoSizeHeight * CmToDIP);
    }

    private Size GetGridPhotoSize(Rect printableArea, IReadOnlyDictionary<string, IPrintOptionDetails> options)
    {
        int columnCount = 0;
        if (options.TryGetValue(PhotoSizeColumnsOption.OptionId, out var columnsOption))
        {
            bool isInt = int.TryParse((string)columnsOption.Value, out columnCount);
            if (isInt && columnCount > 0)
            {
                columnsOption.ErrorText = "";
            }
            else
            {
                columnsOption.ErrorText = Strings.Printing_GridSpecificationInvalidError;
                columnCount = 0;
            }
        }
        int rowCount = 0;
        if (options.TryGetValue(PhotoSizeRowsOption.OptionId, out var rowsOption))
        {
            bool isInt = int.TryParse((string)rowsOption.Value, out rowCount);
            if (isInt && rowCount > 0)
            {
                rowsOption.ErrorText = "";
            }
            else
            {
                rowsOption.ErrorText = Strings.Printing_GridSpecificationInvalidError;
                rowCount = 0;
            }
        }

        if (columnCount == 0 || rowCount == 0)
        {
            return new Size(0, 0);
        }
        else
        {
            return new Size(Math.Max(printableArea.Width / columnCount, 0), Math.Max(printableArea.Height / rowCount, 0));
        }
    }

    private HorizontalAlignment ToHorizontalAlignment(IPrintOptionDetails printOptionDetails)
    {
        switch (printOptionDetails.Value)
        {
            case HorizontalAlignmentOption.Left:
                return HorizontalAlignment.Left;
            case HorizontalAlignmentOption.Center:
                return HorizontalAlignment.Center;
            case HorizontalAlignmentOption.Right:
                return HorizontalAlignment.Right;
            default:
                throw new UnreachableException();
        }
    }
}
