﻿using Microsoft.UI.Xaml.Data;
using System;

namespace PhotoViewerApp.Converters;

public class StringFormatConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.Format((string)parameter, value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
