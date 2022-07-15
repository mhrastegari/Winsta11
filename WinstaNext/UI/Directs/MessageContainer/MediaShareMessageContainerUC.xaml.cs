﻿using InstagramApiSharp.Classes.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using WinstaCore.Services;
using WinstaNext.Views.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WinstaNext.UI.Directs.MessageContainer
{
    public sealed partial class MediaShareMessageContainerUC : MessageContainerUC
    {
        public RelayCommand NavigateToMediaCommand { get; set; }

        public MediaShareMessageContainerUC()
        {
            this.InitializeComponent();
            NavigateToMediaCommand = new(NavigateToMedia);
        }

        void NavigateToMedia()
        {
            var NavigationService =  App.Container.GetService<NavigationService>();
            NavigationService.Navigate(typeof(SingleInstaMediaView), DirectItem.MediaShare);
        }

        protected override void OnDirectItemChanged()
        {
            base.OnDirectItemChanged();
            if (DirectItem.MediaShare.MediaType == InstaMediaType.Image)
            {
                vidMedia.Visibility = Visibility.Collapsed;
                imgMedia.Source = new BitmapImage(new(DirectItem.MediaShare.Images[0].Uri));
            }
            else if(DirectItem.MediaShare.MediaType == InstaMediaType.Video)
            {
                imgMedia.Visibility = Visibility.Collapsed;
                vidMedia.PosterSource = new BitmapImage(new(DirectItem.MediaShare.Images[0].Uri));
                vidMedia.SetPlaybackSource(
                    MediaSource.CreateFromUri(
                        new Uri(DirectItem.MediaShare.Videos[0].Uri, UriKind.RelativeOrAbsolute)));
            }
            else
            {
                var firstSlide = DirectItem.MediaShare.Carousel[0];
                if (firstSlide.MediaType == InstaMediaType.Image)
                {
                    vidMedia.Visibility = Visibility.Collapsed;
                    imgMedia.Source = new BitmapImage(new(firstSlide.Images[0].Uri));
                }
                else if (firstSlide.MediaType == InstaMediaType.Video)
                {
                    imgMedia.Visibility = Visibility.Collapsed;
                    vidMedia.PosterSource = new BitmapImage(new(firstSlide.Images[0].Uri));
                    vidMedia.SetPlaybackSource(
                        MediaSource.CreateFromUri(
                            new Uri(firstSlide.Videos[0].Uri, UriKind.RelativeOrAbsolute)));
                }
            }
        }
    }
}
