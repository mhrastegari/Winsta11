﻿using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinstaNext.Services;
using WinstaNext.Views.Profiles;
using WinstaNext.Views.Stories;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WinstaNext.UI.Stories
{
    //[AddINotifyPropertyChangedInterface]
    public sealed partial class InstaStoryItemPresenterUC : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty StoryProperty = DependencyProperty.Register(
             "Story",
             typeof(InstaStoryItem),
             typeof(InstaStoryItemPresenterUC),
             new PropertyMetadata(null));

        public event PropertyChangedEventHandler PropertyChanged;

        [OnChangedMethod(nameof(OnStoryChanged))]
        public InstaStoryItem Story
        {
            get { return (InstaStoryItem)GetValue(StoryProperty); }
            set { SetValue(StoryProperty, value); }
        }

        RelayCommand<object> NavigateToUserProfileCommand { get; set; }
        AsyncRelayCommand LikeStoryCommand { get; set; }
        AsyncRelayCommand ReplyStoryCommand { get; set; }

        public bool LoadImage { get; set; } = false;
        public bool LoadMediaElement { get; set; } = false;
        public string ReplyText { get; set; }

        public event EventHandler<bool> TimerEnded;
        
        public InstaStoryItemPresenterUC()
        {
            this.InitializeComponent();
            NavigateToUserProfileCommand = new(NavigateToUserProfile);
            LikeStoryCommand = new(LikeStoryAsync);
            ReplyStoryCommand = new(ReplyStoryAsync);
        }

        ~InstaStoryItemPresenterUC()
        {
            NavigateToUserProfileCommand = null;
            LikeStoryCommand = null;
            ReplyStoryCommand = null;
            StopTimer();
        }

        async Task ReplyStoryAsync()
        {
            if (ReplyStoryCommand.IsRunning) return;
            if(string.IsNullOrEmpty(ReplyText)) return;
            using (var Api = App.Container.GetService<IInstaApi>())
            {
                var result = await Api.StoryProcessor.ReplyToStoryAsync(Story.Id,
                    Story.User.Pk,
                    ReplyText,
                    Story.MediaType == InstaMediaType.Image ? InstaSharingType.Photo : InstaSharingType.Video);
                if (!result.Succeeded) throw result.Info.Exception;
                ReplyText = string.Empty;
            }
        }

        async Task LikeStoryAsync()
        {
            if (LikeStoryCommand.IsRunning) return;
            using (var Api = App.Container.GetService<IInstaApi>())
            {
                var isliked = Story.HasLiked;
                Story.HasLiked = !Story.HasLiked;
                IResult<bool> result;
                if (!isliked)
                    result = await Api.StoryProcessor.LikeStoryAsync(Story.Id);
                else result = await Api.StoryProcessor.UnlikeStoryAsync(Story.Id);
                if (!result.Succeeded)
                {
                    Story.HasLiked = isliked;
                    throw result.Info.Exception;
                }
            }
        }

        void NavigateToUserProfile(object obj)
        {
            var NavigationService = App.Container.GetService<NavigationService>();
            NavigationService.Navigate(typeof(UserProfileView), obj);
        }

        void OnStoryChanged()
        {
            LoadMediaElement = LoadImage = false;
            if (Story.MediaType == InstaMediaType.Video)
                LoadMediaElement = true;
            else LoadImage = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadImage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadMediaElement)));
        }

        public void StartTimer()
        {
            if (LoadMediaElement)
            {
                StopExistingTimers();
                if (videoplayer.Source == null)
                    videoplayer.Source = new(Story.Videos[0].Uri);
                videoplayer.Play();
            }
            else
            {
                StopExistingTimers();
                var timer = StoryItemView.StoryTimer;
                timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(7000) };
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        void StopExistingTimers()
        {
            var timer = StoryItemView.StoryTimer;
            if (timer == null) return;
            timer.Tick -= Timer_Tick;
            timer.Stop();
            timer = null;
        }

        public void StopTimer()
        {
            var timer = StoryItemView.StoryTimer;
            if (LoadMediaElement) videoplayer.Stop();
            if (timer == null) return;
            timer.Tick -= Timer_Tick;
            timer.Stop();
            timer = null;
        }

        private void Timer_Tick(object sender, object e)
        {
            TimerEnded?.Invoke(this, true);
        }

        private void videoplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            TimerEnded?.Invoke(this, true);
        }

        private void ReplyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (LoadMediaElement)
                videoplayer.Pause();
            else StopTimer();
        }

        private void ReplyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (LoadMediaElement)
                videoplayer.Play();
            else StartTimer();
        }

        private void Story_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (LoadMediaElement)
                videoplayer.Pause();
            else StopTimer();
        }

        private void Story_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (LoadMediaElement)
                videoplayer.Play();
            else StartTimer();
        }

        private void SendMessageKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            ReplyStoryCommand.Execute(null);
            args.Handled = true;
        }

        private void InstaStoryItemFlyout_Opening(object sender, object e)
        {
            if (LoadMediaElement)
                videoplayer.Pause();
            else StopTimer();
        }

        private void InstaStoryItemFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            if (LoadMediaElement)
                videoplayer.Play();
            else StartTimer();
        }
    }
}
