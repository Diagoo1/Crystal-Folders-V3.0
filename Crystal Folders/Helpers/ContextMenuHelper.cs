using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace CrystalFolders.Helpers
{
    public static class ContextMenuHelper
    {
        #region================== FIELDS & CONSTANTS ==================
        private const string REG_PATH = @"SOFTWARE\CrystalFolders";
        private const string LAST_EXE_PATH_KEY = "LastExePath";
        #endregion

        #region================== PATH HELPERS ==================
        public static string GetCurrentExePath()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    return exePath;
            }
            catch { }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            if (assemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                assemblyPath = assemblyPath.Replace(".dll", ".exe");

            if (File.Exists(assemblyPath))
                return assemblyPath;

            return assemblyPath;
        }

        private static string GetStr(string key, string fallback)
        {
            try
            {
                var val = Application.Current?.TryFindResource(key)?.ToString();
                if (!string.IsNullOrEmpty(val)) return val;
            }
            catch { }
            return fallback;
        }
        #endregion

        #region================== STATUS CHECK ==================
        public static bool IsContextMenuEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\shell\CrystalFolders"))
                {
                    return key != null;
                }
            }
            catch { return false; }
        }

        public static bool IsRegistryPathOutdated()
        {
            try
            {
                if (!IsContextMenuEnabled()) return false;

                string currentExe = GetCurrentExePath();

                using (var cmdKey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Classes\Directory\shell\CrystalFolders\command"))
                {
                    if (cmdKey == null) return false;

                    string registeredCmd = cmdKey.GetValue("")?.ToString() ?? "";

                    if (registeredCmd.StartsWith("\""))
                    {
                        int endQuote = registeredCmd.IndexOf("\"", 1);
                        if (endQuote > 0)
                        {
                            string registeredExe = registeredCmd.Substring(1, endQuote - 1);
                            try
                            {
                                return !string.Equals(
                                    Path.GetFullPath(registeredExe),
                                    Path.GetFullPath(currentExe),
                                    StringComparison.OrdinalIgnoreCase);
                            }
                            catch { return true; }
                        }
                    }
                }
            }
            catch { }
            return false;
        }
        #endregion

        #region================== REGISTRY OPERATIONS ==================
        public static bool UpdateRegistryPaths()
        {
            try
            {
                string exePath = GetCurrentExePath();
                if (!File.Exists(exePath)) return false;

                string customizeText = GetStr("CustomizeWithCrystalFolders", "Customize with Crystal Folders");
                string convertText = GetStr("ConvertToIcon", "Convert to Icon");

                using (var folderKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\Directory\shell\CrystalFolders"))
                {
                    folderKey.SetValue("", customizeText);
                    folderKey.SetValue("Icon", exePath);
                    folderKey.SetValue("MultiSelectModel", "Player");

                    using (var cmdKey = folderKey.CreateSubKey("command"))
                    {
                        cmdKey.SetValue("", $"\"{exePath}\" --folder \"%1\"");
                    }
                }

                using (var bgKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\Directory\Background\shell\CrystalFolders"))
                {
                    bgKey.SetValue("", customizeText);
                    bgKey.SetValue("Icon", exePath);

                    using (var cmdKey = bgKey.CreateSubKey("command"))
                    {
                        cmdKey.SetValue("", $"\"{exePath}\" --folder \"%V\"");
                    }
                }

                string[] imageExts = { ".png", ".jpg", ".jpeg", ".bmp" };
                foreach (string ext in imageExts)
                {
                    using (var imgKey = Registry.CurrentUser.CreateSubKey(
                        $"Software\\Classes\\SystemFileAssociations\\{ext}\\shell\\CrystalFoldersConvert"))
                    {
                        imgKey.SetValue("", convertText);
                        imgKey.SetValue("Icon", exePath);

                        using (var cmdKey = imgKey.CreateSubKey("command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" --convert \"%1\"");
                        }
                    }
                }

                using (var key = Registry.CurrentUser.CreateSubKey(REG_PATH))
                {
                    key.SetValue(LAST_EXE_PATH_KEY, exePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextMenuHelper] UpdateRegistryPaths error: {ex.Message}");
                return false;
            }
        }

        public static bool RemoveContextMenu()
        {
            try
            {
                try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\CrystalFolders", false); } catch { }
                try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\CrystalFolders", false); } catch { }

                string[] imageExts = { ".png", ".jpg", ".jpeg", ".bmp" };
                foreach (string ext in imageExts)
                {
                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(
                            $"Software\\Classes\\SystemFileAssociations\\{ext}\\shell\\CrystalFoldersConvert", false);
                    }
                    catch { }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextMenuHelper] RemoveContextMenu error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region================== AUTO SYNC ==================
        public static void AutoSyncRegistryOnStartup()
        {
            try
            {
                if (!IsContextMenuEnabled()) return;

                if (IsRegistryPathOutdated())
                {
                    Debug.WriteLine("[ContextMenuHelper] EXE path changed, updating registry...");
                    UpdateRegistryPaths();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextMenuHelper] AutoSync error: {ex.Message}");
            }
        }
        #endregion
    }
}