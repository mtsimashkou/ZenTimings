using AdonisUI;
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
        private const int VERSION_MINOR = 1;

        private const string filename = "settings.xml";

        public enum THEME : int
        {
            LIGHT,
            DARK,
            DARK_MINT,
        }

        public AppSettings Create()
        {
            Version = $"{VERSION_MAJOR}.{VERSION_MINOR}";
            AppTheme = THEME.LIGHT;
            AutoRefresh = true;
            AutoRefreshInterval = 2000;
            AdvancedMode = true;
            // DarkMode = false;
            CheckForUpdates = false;
            SaveWindowPosition = false;
            WindowLeft = 0;
            WindowTop = 0;
            SysInfoWindowLeft = 0;
            SysInfoWindowHeight = 0;
            SysInfoWindowWidth = 0;
            NotifiedChangelog = "";
            NotifiedRembrandt = "";
            TM5ExecutablePath = "";

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
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        sr.Close();
                        MessageBox.Show(
                            "Invalid settings file!\nSettings will be reset to defaults.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
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
            catch (Exception)
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
            Uri[] ThemeUri = new Uri[]
            {
                new Uri("pack://application:,,,/ZenTimings;component/Themes/Dark.xaml", UriKind.Absolute),
                new Uri("pack://application:,,,/ZenTimings;component/Themes/DarkMint.xaml", UriKind.Absolute),
                new Uri("pack://application:,,,/ZenTimings;component/Themes/Light.xaml", UriKind.Absolute),
            };

            if (AppTheme == THEME.DARK)
                ResourceLocator.SetColorScheme(Application.Current.Resources, ThemeUri[0]);
            else if (AppTheme == THEME.DARK_MINT)
                ResourceLocator.SetColorScheme(Application.Current.Resources, ThemeUri[1]);
            else
                ResourceLocator.SetColorScheme(Application.Current.Resources, ThemeUri[ThemeUri.Length - 1]);

            //DarkMode = !DarkMode;
        }

        public string Version { get; set; } = $"{VERSION_MAJOR}.{VERSION_MINOR}";
        public bool AutoRefresh { get; set; } = true;
        public int AutoRefreshInterval { get; set; } = 2000;
        public bool AdvancedMode { get; set; } = true;
        // public bool DarkMode { get; set; }
        public THEME AppTheme { get; set; } = THEME.LIGHT;
        public bool CheckForUpdates { get; set; } = true;
        public string UpdaterSkippedVersion { get; set; } = "";
        public string UpdaterRemindLaterAt { get; set; } = "";
        public bool MinimizeToTray { get; set; }
        public bool SaveWindowPosition { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public double SysInfoWindowLeft { get; set; }
        public double SysInfoWindowTop { get; set; }
        public double SysInfoWindowWidth { get; set; }
        public double SysInfoWindowHeight { get; set; }
        public string NotifiedChangelog { get; set; } = "";
        public string NotifiedRembrandt { get; set; } = "";
        public string TM5ExecutablePath { get; set; } = "";
    }
}