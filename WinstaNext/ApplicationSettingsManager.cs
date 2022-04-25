﻿using WinstaNext.Core.Theme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using InstagramApiSharp.API.Builder;
using Newtonsoft.Json;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Security.Cryptography;
using System.IO;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API;
using WinstaNext.Models.Core;
using Windows.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using WinstaNext.Helpers;
using System.Collections;
using WinstaCore;
using System.Globalization;
using Windows.ApplicationModel.Resources.Core;

namespace WinstaNext
{
    internal class ApplicationSettingsManager
    {
        string UserSessionsFolderName { get => "UserSessions"; }

        string AppThemeSetting { get => "AppTheme"; }
        string AppLanguageSettings { get => "AppLanguage"; }
        string AutoPlaySettings { get => "AutoPlay"; }
        string ForceThreeColumnsSettings { get => "ForceThreeColumns"; }
        string LastLoggedUserSettings { get => "LastLoggedUser"; }
        string RemoveFeedAdsSetting { get => "RemoveFeedAds"; }
        string ShowLoginSetting { get => "ShowLogin"; }
        string UserNamesSetting { get => "UserNames"; }

        StorageFolder LocalFolder { get; }
        ApplicationDataContainer LocalSettings { get; }
        ApplicationDataContainer RoamingSettings { get; }

        public static ApplicationSettingsManager Instance { get; }

        static ApplicationSettingsManager()
        {
            Instance = new ApplicationSettingsManager();
        }

        private ApplicationSettingsManager()
        {
            LocalFolder = ApplicationData.Current.LocalFolder;
            LocalSettings = ApplicationData.Current.LocalSettings;
            RoamingSettings = ApplicationData.Current.RoamingSettings;
        }

        public string GetLastLoggedUser()
        {
            if (LocalSettings.Values.TryGetValue(LastLoggedUserSettings, out var LastLoggedUser))
            {
                return LastLoggedUser.ToString();
            }
            else
            {
                var users = GetUsersList();
                if (users == null || !users.Any())
                    return null;
                else return SetLastLoggedUser(users.FirstOrDefault().Key);
            }
        }

        public string SetLastLoggedUser(string userPk)
        {
            LocalSettings.Values[LastLoggedUserSettings] = userPk;
            return userPk;
        }

        public async Task<List<IInstaApi>> GetUsersApiListAsync()
        {
            var users = GetUsersList();
            List<IInstaApi> apis = new List<IInstaApi>();
            for (int i = 0; i < users.Count; i++)
            {
                var user = users.ElementAt(i);
                var session = await GetUserSession(user.Key);
                var api = ((App)App.Current).CreateInstaAPIInstance(session);
                apis.Add(api);
            }
            return apis;
        }

        public LanguageDefinition[] GetSupportedLanguages()
        {
            return new LanguageDefinition[]
            {
                new LanguageDefinition("Default",""),
                new LanguageDefinition("English","en-Us"),
                new LanguageDefinition("Persian (پارسی)","fa-Ir"),
            };
        }

        public bool GetForceThreeColumns()
        {
            if (LocalSettings.Values.TryGetValue(ForceThreeColumnsSettings, out var ForceThreeColumns))
            {
                return Convert.ToBoolean(ForceThreeColumns);
            }
            else
            {
                return SetForceThreeColumns();
            }
        }

        public bool SetForceThreeColumns(bool forceThreeColumns = false)
        {
            LocalSettings.Values[ForceThreeColumnsSettings] = forceThreeColumns;
            return forceThreeColumns;
        }

        public string GetLanguage(bool resetcontextifsetneeded = true)
        {
            if (LocalSettings.Values.TryGetValue(AppLanguageSettings, out var lang))
            {
                return lang.ToString();
            }
            else
            {
                return SetLanguage(resetcontextifsetneeded: resetcontextifsetneeded);
            }
        }

        public string SetLanguage(string lang = "", bool resetcontextifsetneeded = true)
        {
            LocalSettings.Values[AppLanguageSettings] = lang;
            ApplicationLanguages.PrimaryLanguageOverride = lang;
            CultureInfo culture = new(lang);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (resetcontextifsetneeded)
            {
                ResourceContext.GetForCurrentView().Reset();
                ResourceContext.GetForViewIndependentUse().Reset();
            }

            return lang;
        }

        public bool GetAutoPlay()
        {
            if (LocalSettings.Values.TryGetValue(AutoPlaySettings, out var autoplay))
            {
                return Convert.ToBoolean(autoplay);
            }
            else
            {
                return SetAutoPlay();
            }
        }

        public bool SetAutoPlay(bool enabled = true)
        {
            LocalSettings.Values[AutoPlaySettings] = enabled;
            return enabled;
        }

        public bool GetRemoveFeedAds()
        {
            if (LocalSettings.Values.TryGetValue(RemoveFeedAdsSetting, out var removeAds))
            {
                return (bool)removeAds;
            }
            else
            {
                return SetRemoveFeedAds();
            }
        }

        public bool SetRemoveFeedAds(bool removeFeedAds = false)
        {
            LocalSettings.Values[RemoveFeedAdsSetting] = removeFeedAds;
            return removeFeedAds;
        }

        public AppTheme GetTheme()
        {
            if (LocalSettings.Values.TryGetValue(AppThemeSetting, out var theme))
            {
                return Enum.Parse<AppTheme>(theme.ToString());
            }
            else
            {
                SetTheme(AppTheme.Default);
                return AppTheme.Default;
            }
        }

        public void SetTheme(AppTheme appTheme)
        {
            LocalSettings.Values[AppThemeSetting] = appTheme.ToString();
        }

        public Dictionary<string, string> GetUsersList()
        {
            if (LocalSettings.Values.TryGetValue(UserNamesSetting, out var users))
            {
                if (users != null)
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(users.ToString());
                }
                else
                {
                    return SetUsersList();
                }
            }
            else
            {
                return SetUsersList();
            }
        }

        public async Task AddOrUpdateUser(long pk, string session, string username)
        {
            await AddOrUpdateUser(pk.ToString(), session, username);
        }

        async Task AddOrUpdateUser(string pk, string session, string username)
        {
            await SetUserSession(pk, session);
            var users = GetUsersList();
            if (!users.ContainsKey(pk))
                users.Add(pk, username);
            else users[pk] = username;
            SetUsersList(users);
        }

        private Dictionary<string, string> SetUsersList(Dictionary<string, string> users = null)
        {
            if (users == null) users = new Dictionary<string, string>();
            LocalSettings.Values[UserNamesSetting] = JsonConvert.SerializeObject(users);
            return users;
        }

        public bool GetShowLoginScreen()
        {
            var users = GetUsersList();
            if (users.Count == 0) return true;
            return false;
        }

        public async Task<string> GetUserSession(string userPk)
        {
            var folder = await LocalFolder.CreateFolderAsync(UserSessionsFolderName, CreationCollisionOption.OpenIfExists);
            try
            {
                var file = await folder.GetFileAsync(userPk);
                var str = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                return CryptoHelper.DecryptString(str);
                //using (var read = await file.OpenReadAsync())
                //{
                //    var str = await read.ReadTextAsync(Encoding.UTF8);
                //    return DecryptString(SessionEncryptionKey, str);
                //}
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SetUserSession(long userPk, string session)
        {
            await SetUserSession(userPk.ToString(), session);
        }

        async Task SetUserSession(string userPk, string session)
        {
            var folder = await LocalFolder.CreateFolderAsync(UserSessionsFolderName, CreationCollisionOption.OpenIfExists);
            try
            {
                ((App)App.Current).SetCurrentUserSession(session);
                var file = await folder.CreateFileAsync(userPk, CreationCollisionOption.OpenIfExists);
                session = CryptoHelper.EncryptString(session);
                await FileIO.WriteTextAsync(file, session, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
