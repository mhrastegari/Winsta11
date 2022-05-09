﻿using InstagramApiSharp.Classes.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using PropertyChanged;
using System.ComponentModel;
using System.Threading;
using Windows.System;

namespace WinstaNext.Abstractions.Stories
{
    [AddINotifyPropertyChangedInterface]
    public class WinstaStoryItem : INotifyPropertyChanged
    {
        private WinstaStoryItem() { }

        public WinstaStoryItem(InstaBroadcast broadcast)
        {
            Broadcast = broadcast;
        }

        public WinstaStoryItem(InstaHashtagStory hashtagStory)
        {
            HashtagStory = hashtagStory;
        }

        public WinstaStoryItem(InstaReelFeed reelFeed)
        {
            ReelFeed = new(reelFeed);
        }

        public InstaBroadcast Broadcast { get; }
        public InstaHashtagStory HashtagStory { get; }
        public WinstaReelFeed ReelFeed { get; }

        public bool IsSelected { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
