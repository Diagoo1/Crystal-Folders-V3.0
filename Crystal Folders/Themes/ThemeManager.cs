using System;
using System.Linq;
using System.Windows;

namespace CrystalFolders.Themes
{
    public static class ThemeManager
    {
        #region================== FIELDS & PROPERTIES ==================
        public static bool IsDarkMode { get; private set; } = false;
        #endregion

        #region================== INITIALIZATION ==================
        public static void Initialize()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\CrystalFolders"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("DarkMode");
                        if (val != null)
                            IsDarkMode = Convert.ToBoolean(val);
                    }
                }
            }
            catch { }

            ApplyTheme();
        }
        #endregion

        #region================== THEME MANAGEMENT ==================
        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme();

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\CrystalFolders"))
                {
                    key.SetValue("DarkMode", IsDarkMode);
                }
            }
            catch { }
        }

        private static void ApplyTheme()
        {
            var res = Application.Current.Resources;
            string sfx = IsDarkMode ? "Dark" : "";

            string[] keys = {
                "WindowBg", "SidebarBg", "CardBg", "MainText", "SubText",
                "Accent", "BorderBrush", "Border", "HoverBg", "TotalCardBg",
                "Success", "Warning", "Error", "Info", "Purple", "Pink",
                "Indigo", "Teal", "Violet", "Fuchsia",
                "BlueLight", "BlueMedium", "BlueViolet", "PurpleLight"
            };

            foreach (var k in keys)
            {
                var sourceKey = k + sfx;
                var targetKey = "Dynamic" + k;
                if (res.Contains(sourceKey) && res.Contains(targetKey))
                    res[targetKey] = res[sourceKey];
            }
        }
        #endregion

        #region================== LANGUAGE MANAGEMENT ==================
        public static void ApplyLanguage(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode)) langCode = "en";
            langCode = langCode.ToLower();

            string fileName;
            switch (langCode)
            {
                case "ar": fileName = "Lang-AR.xaml"; break;
                case "es": fileName = "Lang-ES.xaml"; break;
                case "fr": fileName = "Lang-FR.xaml"; break;
                case "ru": fileName = "Lang-RU.xaml"; break;
                default: fileName = "Lang-EN.xaml"; langCode = "en"; break;
            }

            var dict = new ResourceDictionary
            {
                Source = new Uri($"Lang/{fileName}", UriKind.Relative)
            };

            var oldLangs = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source != null && d.Source.OriginalString.Contains("Lang/Lang-"))
                .ToList();
            foreach (var d in oldLangs)
                Application.Current.Resources.MergedDictionaries.Remove(d);

            Application.Current.Resources.MergedDictionaries.Add(dict);

            var flow = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Application.Current.Resources["AppFlowDirection"] = flow;
            Application.Current.Resources["CurrentLanguage"] = langCode;

            foreach (Window w in Application.Current.Windows)
                w.FlowDirection = flow;
        }

        public static string CurrentLanguage
            => Application.Current.Resources["CurrentLanguage"] as string ?? "en";
        #endregion
    }
}