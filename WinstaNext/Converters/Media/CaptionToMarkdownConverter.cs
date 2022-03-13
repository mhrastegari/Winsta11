﻿using System;
using Windows.UI.Xaml.Data;
using WinstaNext.Helpers;

namespace WinstaNext.Converters.Media
{
    internal class CaptionToMarkdownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not string caption) return string.Empty;
            return caption.ToMarkdownText();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
