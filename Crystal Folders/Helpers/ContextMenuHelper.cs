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
                string[] keysToCheck =
                {
            @"Software\Classes\Directory\shell\CrystalFolders",
            @"Software\Classes\Directory\Background\shell\CrystalFolders"
        };

                foreach (var path in keysToCheck)
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (k != null) return true;
                    }
                }

                string[] imageExts = { ".png", ".jpg", ".jpeg", ".bmp" };
                foreach (string ext in imageExts)
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(
                        $@"Software\Classes\SystemFileAssociations\{ext}\shell\CrystalFoldersConvert"))
                    {
                        if (k != null) return true;
                    }
                }
            }
            catch { }
            return false;
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
                NotifyShellChange();
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
            bool anyRemoved = false;

            try
            {
                // 1) Directory (Folder) context menu
                anyRemoved |= SafeDeleteKey(Registry.CurrentUser,
                    @"Software\Classes\Directory\shell\CrystalFolders");

                // 2) Directory Background (right-click in empty area)
                anyRemoved |= SafeDeleteKey(Registry.CurrentUser,
                    @"Software\Classes\Directory\Background\shell\CrystalFolders");

                // 3) Image extensions
                string[] imageExts = { ".png", ".jpg", ".jpeg", ".bmp" };
                foreach (string ext in imageExts)
                {
                    anyRemoved |= SafeDeleteKey(Registry.CurrentUser,
                        $@"Software\Classes\SystemFileAssociations\{ext}\shell\CrystalFoldersConvert");

                    anyRemoved |= SafeDeleteKey(Registry.CurrentUser,
                        $@"Software\Classes\{ext}\shell\CrystalFoldersConvert");
                }

                TryDeleteFromLocalMachine();

                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(REG_PATH))
                    {
                        key?.DeleteValue(LAST_EXE_PATH_KEY, false);
                    }
                }
                catch { }

                NotifyShellChange();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextMenuHelper] RemoveContextMenu error: {ex.Message}");
                return anyRemoved;
            }
        }

        // ============ Helpers ============

        private static bool SafeDeleteKey(RegistryKey root, string subKey)
        {
            try
            {
                using (var check = root.OpenSubKey(subKey))
                {
                    if (check == null) return false;
                }

                root.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);

                using (var verify = root.OpenSubKey(subKey))
                {
                    return verify == null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SafeDeleteKey] {subKey} → {ex.Message}");
                return false;
            }
        }

        private static void TryDeleteFromLocalMachine()
        {
            string[] hklmPaths =
            {
        @"Software\Classes\Directory\shell\CrystalFolders",
        @"Software\Classes\Directory\Background\shell\CrystalFolders",
    };

            foreach (var path in hklmPaths)
            {
                try
                {
                    using (var check = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (check == null) continue;
                    }
                    Registry.LocalMachine.DeleteSubKeyTree(path, false);
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[HKLM] Need admin rights for: {path}");
                }
                catch { }
            }
        }

        // ============ Shell Notification ============
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_IDLIST = 0x0000;

        private static void NotifyShellChange()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
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