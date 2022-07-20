﻿using InstagramApiSharp.Classes.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace WinstaNext.Converters.Directs
{
    internal class UserIdToProfilePictureVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var me = App.Container.GetService<InstaUserShort>();
            if (value is long val)
            {
                if (val == me.Pk) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
