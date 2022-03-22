﻿
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinstaNext.Core.Collections;
using WinstaNext.Core.Messages;
using WinstaNext.Core.Theme;
using WinstaNext.Models.Core;
using WinstaNext.Views;
using WinstaNext.Views.Settings;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using PropertyChanged;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using InstagramApiSharp.API;
using Microsoft.Extensions.DependencyInjection;
using InstagramApiSharp.Classes.Models;
using WinstaNext.Views.Directs;
using System.Diagnostics;
using InstagramApiSharp;
using WinstaNext.Views.Profiles;
using WinstaNext.Views.Search;
using InstagramApiSharp.API.Push;
using WinstaBackgroundHelpers.Push;
using NotificationHandler;
using WinstaNext.Views.Activities;
using Windows.UI.Xaml.Input;
using Windows.System;
using Windows.ApplicationModel.Resources;
using System.Globalization;

namespace WinstaNext.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        ThemeListener _themeListener;
        public string SearchQuery { get; set; }
        public string WindowTitle { get; set; } = LanguageManager.Instance.General.ApplicationName;
        public bool IsNavigationViewPaneOpened { get; set; }

        public RelayCommand ToggleNavigationViewPane { get; }
        public AsyncRelayCommand<AutoSuggestBoxTextChangedEventArgs> SearchBoxTextChangedCommand { get; }
        public RelayCommand<AutoSuggestBoxQuerySubmittedEventArgs> SearchBoxQuerySubmittedCommand { get; }
        public RelayCommand<AutoSuggestBoxSuggestionChosenEventArgs> SearchBoxSuggestionChosenCommand { get; }
        public RelayCommand<NavigationEventArgs> FrameNavigatedCommand { get; }
        public RelayCommand<object> NavigateToUserProfileCommand { get; }

        /// <summary>
        /// Items at the top of the NavigationView.
        /// </summary>
        internal ExtendedObservableCollection<MenuItemModel> MenuItems { get; } = new();

        /// <summary>
        /// Gets or sets the list of items to displayed in the Search Box after a search.
        /// </summary>
        internal ExtendedObservableCollection<InstaUser> SearchResults { get; } = new();

        /// <summary>
        /// Items at the bottom of the NavigationView.
        /// </summary>
        internal ExtendedObservableCollection<MenuItemModel> FooterMenuItems { get; } = new();

        /// <summary>
        /// Gets or sets the selected menu item in the NavitationView.
        /// </summary>
        [OnChangedMethod(nameof(SelectedMenuItemChanged))]
        internal MenuItemModel SelectedMenuItem { get; set; }

        public Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }

        [OnChangedMethod(nameof(OnInstaUserChanged))]
        public InstaUserShort InstaUser { get; private set; }

        public override string PageHeader { get; protected set; }

        IInstaApi PushClientApi { get; set; }

        public static MainPageViewModel mainPageViewModel = null;
        SystemNavigationManager SystemNavigationManager = null;
        public MainPageViewModel()
        {
            mainPageViewModel = this;

            SearchBoxTextChangedCommand = new(SearchBoxTextChanged);
            SearchBoxQuerySubmittedCommand = new(SearchBoxQuerySubmitted);
            SearchBoxSuggestionChosenCommand = new(SearchBoxSuggestionChosen);

            FrameNavigatedCommand = new(FrameNavigated);
            NavigateToUserProfileCommand = new(NavigateToUserProfile);
            _themeListener = new();
            SetupTitlebar(CoreApplication.GetCurrentView().TitleBar);
            MenuItems.Add(new(LanguageManager.Instance.General.Home, "\uE10F", typeof(HomeView)));
            MenuItems.Add(new(LanguageManager.Instance.Instagram.Activities, "\uE006", typeof(ActivitiesView)));
            MenuItems.Add(new(LanguageManager.Instance.Instagram.Explore, "\uF6FA", null));
            MenuItems.Add(new(LanguageManager.Instance.Instagram.Directs, "\uE15F", typeof(DirectsListView)));
            FooterMenuItems.Add(new(LanguageManager.Instance.General.Settings, new AnimatedSettingsVisualSource(), typeof(SettingsView)));
            ToggleNavigationViewPane = new(ToggleNavigationPane);
            _themeListener.ThemeChanged += MainPageViewModel_ThemeChanged;

            new Thread(GetDirectsCountAsync).Start();
            new Thread(SyncLauncher).Start();
            new Thread(GetMyUser).Start();
            SystemNavigationManager = SystemNavigationManager.GetForCurrentView();
            SystemNavigationManager.BackRequested += MainPageViewModel_BackRequested;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == VirtualKey.Escape)
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
            args.Handled = true;
        }

        private void MainPageViewModel_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                e.Handled = true;
            }
        }

        ~MainPageViewModel()
        {
            mainPageViewModel = null;
        }

        async void GetDirectsCountAsync()
        {
            using (IInstaApi Api = App.Container.GetService<IInstaApi>())
            {
                var result = await Api.MessagingProcessor
                                      .GetDirectInboxAsync(PaginationParameters.MaxPagesToLoad(1));
                if (!result.Succeeded) throw result.Info.Exception;
                var value = result.Value;
                var count = value.PendingRequestsCount + value.Inbox.UnseenCount;
                
                UIContext.Post((e) =>
                {
                    var DirectsText = LanguageManager.Instance.Instagram.Directs;
                    var menu = MenuItems.FirstOrDefault(x => x.Text == DirectsText);
                    menu.Badge = count.ToString();
                }, null);
            }
        }

        async void StartPushClient()
        {
            var apis = await ApplicationSettingsManager.Instance.GetUsersApiListAsync();
            PushClientApi = App.Container.GetService<IInstaApi>();

            PushClientApi.PushClient = new PushClient(apis, PushClientApi);
            PushClientApi.PushClient.MessageReceived += PushClient_MessageReceived;
            await PushClientApi.PushProcessor.RegisterPushAsync();
            PushClientApi.PushClient.Start();
        }

        public void StopPushClient()
        {
            PushClientApi.PushClient.MessageReceived -= PushClient_MessageReceived;
            PushClientApi.PushClient.Shutdown();
            PushClientApi.Dispose();
        }

        public void RemoveNavigationEvents()
        {
            SystemNavigationManager.BackRequested -= MainPageViewModel_BackRequested;
            SystemNavigationManager = null;
        }

        async void PushClient_MessageReceived(object sender, PushReceivedEventArgs e)
        {
            if (e == null || e.NotificationContent == null) return;

            UIContext.Post((s) =>
            {
                var directsmenu = MenuItems.FirstOrDefault(x => x.Text == LanguageManager.Instance.Instagram.Directs);
                directsmenu.Badge = $"{e.NotificationContent.BadgeCount.Direct}";

                var activitiesmenu = MenuItems.FirstOrDefault(x => x.Text == LanguageManager.Instance.Instagram.Activities);
                activitiesmenu.Badge = $"{e.NotificationContent.BadgeCount.Activities}";
            }, null);

            var apis = await ApplicationSettingsManager.Instance.GetUsersApiListAsync();
            PushHelper.HandleNotify(e.NotificationContent, apis);
        }

        async void SyncLauncher()
        {
            using (IInstaApi Api = App.Container.GetService<IInstaApi>())
            {
                await Api.LauncherSyncAsync();
                await Api.PushProcessor.RegisterPushAsync();
            }
            StartPushClient();
        }

        async void GetMyUser()
        {
            using (IInstaApi Api = App.Container.GetService<IInstaApi>())
            {
                var result = await Api.UserProcessor.GetCurrentUserAsync();
                UIContext.Post((a) =>
                {
                    if (!result.Succeeded)
                    {
                        InstaUser = App.Container.GetService<InstaUserShort>();
                        ApplicationSettingsManager.Instance.SetLastLoggedUser(InstaUser.Pk.ToString());
                        return;
                    }
                    InstaUser = result.Value;
                    ((App)App.Current).SetMyUserInstance(result.Value);
                    ApplicationSettingsManager.Instance.SetLastLoggedUser(result.Value.Pk.ToString());
                }, null);
            }
        }
        async void OnInstaUserChanged()
        {
            if (InstaUser == null) return;
            using (IInstaApi Api = App.Container.GetService<IInstaApi>())
            {
                Api.UpdateUser(InstaUser);
                var state = Api.GetStateDataAsString();
                await ApplicationSettingsManager.Instance.
                            AddOrUpdateUser(InstaUser.Pk, state, InstaUser.UserName);
            }
        }

        void NavigateToUserProfile(object obj)
        {
            NavigationService.Navigate(typeof(UserProfileView), obj);
        }

        bool SuggestionChosen = false;
        private void SearchBoxSuggestionChosen(AutoSuggestBoxSuggestionChosenEventArgs arg)
        {
            SuggestionChosen = true;
            var user = (InstaUser)arg.SelectedItem;
            NavigationService.Navigate(typeof(UserProfileView), user);
            SearchQuery = string.Empty;
        }

        private void SearchBoxQuerySubmitted(AutoSuggestBoxQuerySubmittedEventArgs arg)
        {
            if (SuggestionChosen) { SuggestionChosen = false; return; }
            if (string.IsNullOrEmpty(SearchQuery)) return;
            NavigationService.Navigate(typeof(SearchView), SearchQuery);
            SearchQuery = string.Empty;
        }

        Stopwatch stopwatch = null;
        private async Task SearchBoxTextChanged(AutoSuggestBoxTextChangedEventArgs arg)
        {
            if (arg.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (stopwatch == null)
                stopwatch = Stopwatch.StartNew();
            else stopwatch.Restart();
            await Task.Delay(400);
            if (stopwatch.ElapsedMilliseconds < 400) return;
            try
            {
                using (IInstaApi Api = App.Container.GetService<IInstaApi>())
                {
                    var result = await Api.DiscoverProcessor.SearchPeopleAsync(SearchQuery,
                       PaginationParameters.MaxPagesToLoad(1));
                    if (result.Succeeded)
                    {
                        SearchResults.Clear();
                        SearchResults.AddRange(result.Value.Users);
                    }
                }
            }
            finally { }
        }

        void ToggleNavigationPane()
        {
            IsNavigationViewPaneOpened = !IsNavigationViewPaneOpened;
        }

        bool ignoreSetMenuItem = false;
        private void FrameNavigated(NavigationEventArgs obj)
        {
            switch (obj.Content.GetType().Name)
            {
                case "HomeView":
                    SelectedMenuItem = MenuItems.FirstOrDefault(x => x.View == typeof(HomeView));
                    break;

                case "ActivitiesView":
                    SelectedMenuItem = MenuItems.FirstOrDefault(x => x.View == typeof(ActivitiesView));
                    break;

                case "DirectsListView":
                    SelectedMenuItem = MenuItems.FirstOrDefault(x => x.View == typeof(DirectsListView));
                    break;

                case "SettingsView":
                    SelectedMenuItem = FooterMenuItems.FirstOrDefault(x => x.View == typeof(SettingsView));
                    break;

                default:
                    break;
            }
        }

        private void MainPageViewModel_ThemeChanged(ThemeListener sender)
        {
            UIContext.Post(new SendOrPostCallback(ApplyThemeForTitleBarButtons), null);
        }

        void SelectedMenuItemChanged()
        {
            if (SelectedMenuItem == null) return;
            if (ignoreSetMenuItem) { ignoreSetMenuItem = false; return; }
            if (NavigationService.Content != null && NavigationService.Content.GetType() == SelectedMenuItem.View) return;
            if (SelectedMenuItem.View != null)
            {
                WeakReferenceMessenger.Default.Send(new NavigateToPageMessage(SelectedMenuItem.View));
            }
        }

        void SetupTitlebar(CoreApplicationViewTitleBar coreTitleBar)
        {
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            ApplyThemeForTitleBarButtons();
        }

        private void ApplyThemeForTitleBarButtons(object noUse = null)
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            var theme = ApplicationSettingsManager.Instance.GetTheme();

            if (theme == AppTheme.Default)
            {
                theme = _themeListener.CurrentTheme == ApplicationTheme.Light ? AppTheme.Light : AppTheme.Dark;
            }

            if (theme == AppTheme.Dark)
            {
                // Set active window colors
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 90, 90, 90);
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 120, 120, 120);

                // Set inactive window colors
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.InactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                titleBar.BackgroundColor = Color.FromArgb(255, 45, 45, 45);
            }
            else if (theme == AppTheme.Light)
            {
                // Set active window colors
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 180, 180, 180);
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 150, 150, 150);

                // Set inactive window colors
                titleBar.InactiveForegroundColor = Colors.DimGray;
                titleBar.InactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Colors.DimGray;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                titleBar.BackgroundColor = Color.FromArgb(255, 210, 210, 210);
            }
        }


        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            SetupTitlebar(sender);
        }

    }
}
