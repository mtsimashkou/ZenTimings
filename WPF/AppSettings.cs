﻿using AdonisUI;
using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace ZenTimings
{
    [Serializable]
    public sealed class AppSettings
    {
        private const int VERSION_MAJOR = 1;
        private const int VERSION_MINOR = 0;

        private const string filename = "settings.xml";

        public AppSettings Create()
        {
            AutoRefresh = true;
            AutoRefreshInterval = 2000;
            AdvancedMode = true;
            DarkMode = false;
            CheckForUpdates = true;
            NotifiedForAutoUpdate = false;

            Save();

            return this;
        }

        public AppSettings Reset() => Create();

        public AppSettings Load()
        {
            if (File.Exists(filename))
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    try
                    {
                        XmlSerializer xmls = new XmlSerializer(typeof(AppSettings));
                        return xmls.Deserialize(sr) as AppSettings;
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                        sr.Close();
                        AdonisUI.Controls.MessageBox.Show(
                            "Invalid settings file!\nSettings will be reset to defaults.",
                            "Error",
                            AdonisUI.Controls.MessageBoxButton.OK,
                            AdonisUI.Controls.MessageBoxImage.Error);
                        return Create();
                    }
                }
            }
            else
            {
                return Create();
            }
        }

        public void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    XmlSerializer xmls = new XmlSerializer(typeof(AppSettings));
                    xmls.Serialize(sw, this);
                }
            }
            catch(Exception ex)
            {
                AdonisUI.Controls.MessageBox.Show(
                    "Could not save settings to file!",
                    "Error",
                    AdonisUI.Controls.MessageBoxButton.OK,
                    AdonisUI.Controls.MessageBoxImage.Error);
            }
        }

        public void ChangeTheme()
        {
            Uri DarkColorScheme = new Uri("pack://application:,,,/ZenTimings;component/Themes/Dark.xaml", UriKind.Absolute);
            Uri LightColorScheme = new Uri("pack://application:,,,/ZenTimings;component/Themes/Light.xaml", UriKind.Absolute);

            if (DarkMode)
                ResourceLocator.SetColorScheme(Application.Current.Resources, DarkColorScheme);
            else
                ResourceLocator.SetColorScheme(Application.Current.Resources, LightColorScheme);

            //DarkMode = !DarkMode;
        }

        public bool AutoRefresh { get; set; } = true;
        public int AutoRefreshInterval { get; set; } = 2000;
        public bool AdvancedMode { get; set; } = true;
        public bool DarkMode { get; set; }
        public bool CheckForUpdates { get; set; } = true;
        public string UpdaterSkippedVersion { get; set; } = "";
        public string UpdaterRemindLaterAt { get; set; } = "";
        public bool NotifiedForAutoUpdate { get; set; }
    }
}
