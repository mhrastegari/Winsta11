﻿using PropertyChanged;
using SecondaryViewsHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace WinstaNext.Services
{
    public class NavigationService : INotifyPropertyChanged
    {
        CoreWindow _cireWindow;
        Frame Frame { get; set; }

        public NavigationService(Frame frame)
        {
            SetNavigationFrame(frame);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            CanGoBack = Frame.CanGoBack;
            CanGoForward = Frame.CanGoForward;
        }

        public void SetNavigationFrame(Frame frame)
        {
            if (frame == null) return;
            if (Frame != null)
            {
                Frame.BackStack.Clear();
                Frame.Navigated -= Frame_Navigated;
            }
            Frame = frame;
            Frame.Navigated += Frame_Navigated;
            _cireWindow = CoreWindow.GetForCurrentThread();
        }

        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public object Content { get => Frame.Content; }

        public IList<PageStackEntry> BackStack { get => Frame.BackStack; }

        public void GoBack() => Frame.GoBack();
        public void GoForward() => Frame.GoForward();

        public bool Navigate(Type sourcePageType)
        {
            //if (_cireWindow.GetKeyState(VirtualKey.Shift).
            //    HasFlag(CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked))
            //{
            //    OpenNewWindow(sourcePageType);
            //    return true;
            //}
            return Frame.Navigate(sourcePageType);
        }

        public bool Navigate(Type sourcePageType, object parameter)
        {
            //if (_cireWindow.GetKeyState(VirtualKey.Shift).
            //    HasFlag(CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked))
            //{
            //    OpenNewWindow(sourcePageType, parameter);
            //    return true;
            //}
            return Frame.Navigate(sourcePageType, parameter);
        }

        public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride)
        {
            //if (_cireWindow.GetKeyState(VirtualKey.Shift).
            //    HasFlag(CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked))
            //{
            //    OpenNewWindow(sourcePageType, parameter);
            //    return true;
            //}
            return Frame.Navigate(sourcePageType, parameter, infoOverride);
        }

        async void OpenNewWindow(Type sourcePageType, object parameter = null)
        {
            await OpenPageAsWindowAsync(sourcePageType, parameter);
        }

        /// <summary>
        /// Opens a page given the page type as a new window.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        async Task<bool> OpenPageAsWindowAsync(Type t, object parameter = null)
        {
            var res = await WindowManagerService.CreateViewLifetimeControlAsync(string.Empty, t, parameter);
            res.Released += Lifetimecontrol_Released;
            return await ApplicationViewSwitcher.TryShowAsStandaloneAsync(res.Id);
        }

        async void Lifetimecontrol_Released(object sender, EventArgs e)
        {
            var control = sender as ViewLifetimeControl;
            control.Released -= Lifetimecontrol_Released;
            await control.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Window.Current.Close();
            });
        }
    }
}
