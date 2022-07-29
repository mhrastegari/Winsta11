﻿using InstagramApiSharp.Classes.Models;
using System;
using Windows.UI.Xaml.Data;
#nullable enable

namespace WinstaNext.Converters.Media
{
    public class InstaMediaThumbnailConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not InstaMedia media) return null;
            switch (media.MediaType)
            {
                case InstaMediaType.Image: return new Uri(media.Images[0].Uri);
                case InstaMediaType.Video: return new Uri(media.Images[0].Uri);
                case InstaMediaType.Carousel: return new Uri(media.Carousel[0].Images[0].Uri);
                default: throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}