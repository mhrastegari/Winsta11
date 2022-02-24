﻿namespace WinstaNext.Core.Messages
{
    public class ChangePageHeaderMessage
    {
        public bool ShowHeader { get; }
        public string Title { get;}
        public ChangePageHeaderMessage(bool showheader, string header)
        {
            ShowHeader = showheader;
            Title = header;
        }
    }
}