using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CrystalFolders.Helpers;
using CrystalFolders.Themes;

namespace CrystalFolders
{
    public partial class MainWindow : Window
    {
        #region================== FIELDS ==================
        public static string icoPath;
        public static bool isPortable = false;
        public static bool isRestore = false;
        public static ObservableCollection<string> folderList = new ObservableCollection<string>();
        public static List<string> subfolderList = new List<string>();

        //----------------- PROPERTIES -----------------
        private bool _isUpdatingSubFolders = false;
        #endregion

        #region================== WIN32 API ==================
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        public static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(ref IMAGELISTDRAWPARAMS pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, out IntPtr picon);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x, y, cx, cy, xBitmap, yBitmap, rgbBk, rgbFg, fStyle, dwRop, fState, Frame, crEffect;
        }

        const int SHIL_JUMBO = 0x4;
        const uint SHGFI_SYSICONINDEX = 0x4000;
        #endregion

        #region================== INITIALIZATION ==================
        public MainWindow()
        {
            InitializeComponent();
            DropList.ItemsSource = folderList;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThemeIcon();
            UpdateSubFolderButtonText();
        }
        #endregion

        #region================== WINDOW & THEME ==================
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsTrayEnabled && App.TrayManager != null && App.TrayManager.IsEnabled)
            {
                this.ShowInTaskbar = false;
                this.Hide();
                ToastManager.Info(GetStr("MinimizedToTray"));
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            UpdateThemeIcon();
            if (DropList.SelectedItem != null) UpdatePreviewFromSelectedFolder();
            ToastManager.Info(ThemeManager.IsDarkMode ? GetStr("DarkModeOn") : GetStr("LightModeOn"));
        }

        private void UpdateThemeIcon()
        {
            if (ThemeManager.IsDarkMode)
            {
                SunIcon.Visibility = Visibility.Visible;
                MoonIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                SunIcon.Visibility = Visibility.Collapsed;
                MoonIcon.Visibility = Visibility.Visible;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new Settings { Owner = this };
            settingsWin.ShowDialog();
            UpdateThemeIcon();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWin = new About { Owner = this };
            aboutWin.ShowDialog();
        }
        #endregion

        #region================== UTILITIES ==================
        //----------------- STRING HELPERS -----------------
        private string GetStr(string key) => Application.Current.TryFindResource(key)?.ToString() ?? key;

        //----------------- COUNT HELPERS -----------------
        private void NCount()
        {
            Dot.Text = folderList.Count.ToString();
            Dotsub.Text = subfolderList.Count.ToString();
        }

        private int CountSubfoldersInPath(string path)
        {
            var uniqueDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SafeGetDirectoriesForSingle(path, uniqueDirs);
            return uniqueDirs.Count;
        }

        //----------------- DIRECTORY SCANNING -----------------
        private void SafeGetDirectoriesForSingle(string path, HashSet<string> uniqueDirs)
        {
            try
            {
                foreach (string dir in Directory.EnumerateDirectories(path))
                {
                    if (uniqueDirs.Add(dir))
                        SafeGetDirectoriesForSingle(dir, uniqueDirs);
                }
            }
            catch { }
        }

        private void SafeGetDirectories(string path, HashSet<string> uniqueDirs, HashSet<string> mainFolders)
        {
            try
            {
                foreach (string dir in Directory.EnumerateDirectories(path))
                {
                    if (!mainFolders.Contains(dir) && uniqueDirs.Add(dir))
                        SafeGetDirectories(dir, uniqueDirs, mainFolders);
                }
            }
            catch { }
        }

        private void AddSubFolders()
        {
            var uniqueSubfolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mainFolders = new HashSet<string>(folderList, StringComparer.OrdinalIgnoreCase);

            foreach (string path in folderList)
                SafeGetDirectories(path, uniqueSubfolders, mainFolders);

            subfolderList = uniqueSubfolders.ToList();
            NCount();
        }

        //----------------- UI RESET -----------------
        private void ResetUI()
        {
            folderList.Clear();
            subfolderList.Clear();
            icoPath = null;
            isRestore = false;
            SlidePortable.IsChecked = false;
            SlideSub.IsChecked = false;

            RestoreText.SetResourceReference(TextBlock.TextProperty, "Restore");
            CustomizeText.SetResourceReference(TextBlock.TextProperty, "ApplyCustomization");
            RestoreIcon.Text = "\uE777";
            CustomizeIcon.Text = "\uE790";

            ChooseBtn.IsEnabled = true;
            SlideSub.IsEnabled = true;
            SlidePortable.IsEnabled = true;
            IconPreviewArea.Cursor = Cursors.Hand;

            UpdateSubFolderButtonText();
            ResetIconPreview();
            NCount();
        }
        #endregion

        #region================== UI ACTIONS ==================
        //----------------- BROWSE & SELECT -----------------
        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserEx.FolderBrowserDialog
            {
                Title = GetStr("SelectFolders"),
                AllowMultiSelect = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int addedCount = 0;
                int duplicateCount = 0;

                foreach (string path in dialog.SelectedFolders)
                {
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        if (!folderList.Contains(path))
                        {
                            folderList.Add(path);
                            addedCount++;
                        }
                        else
                        {
                            duplicateCount++;
                        }
                    }
                }

                if (SlideSub.IsChecked == true) AddSubFolders();
                NCount();

                if (addedCount > 0)
                    ToastManager.Success($"{GetStr("FoldersAdded")} ({addedCount})");

                if (duplicateCount > 0)
                    ToastManager.Warning($"{GetStr("DuplicatesSkipped")} ({duplicateCount})");
            }
        }

        //----------------- REMOVE OPERATIONS -----------------
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (folderList.Count == 0)
            {
                ToastManager.Warning(GetStr("ListAlreadyEmpty"));
                return;
            }

            if (DropList.SelectedItems.Count == 0)
            {
                ToastManager.Warning(GetStr("PleaseSelectItems"));
                return;
            }

            var selected = DropList.SelectedItems.Cast<string>().ToList();
            int removedCount = selected.Count;

            foreach (string s in selected) folderList.Remove(s);

            if (SlideSub.IsChecked == true) AddSubFolders();
            else NCount();

            if (DropList.SelectedItem == null) ResetIconPreview();

            ToastManager.Success($"{GetStr("ItemsRemoved")} ({removedCount})");
        }

        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            if (folderList.Count == 0)
            {
                ToastManager.Warning(GetStr("ListAlreadyEmpty"));
                return;
            }

            int totalCount = folderList.Count;
            folderList.Clear();
            subfolderList.Clear();
            NCount();
            ResetIconPreview();

            ToastManager.Info($"{GetStr("FoldersListCleared")} ({totalCount})");
        }

        //----------------- CACHE MANAGEMENT -----------------
        private async void ClearCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            bool confirmed = await ShowClearCacheConfirmDialog();
            if (!confirmed) return;

            ClearCacheBtn.IsEnabled = false;
            ToastManager.Info(GetStr("ClearingCache"));

            bool success = await Task.Run(() => ClearIconCache());

            ClearCacheBtn.IsEnabled = true;

            if (success)
                ToastManager.Success(GetStr("CacheClearedSuccess"));
            else
                ToastManager.Error(GetStr("CacheClearFailed"));
        }

        private bool ClearIconCache()
        {
            try
            {
                var explorerProcesses = System.Diagnostics.Process.GetProcessesByName("explorer");
                foreach (var proc in explorerProcesses)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(5000);
                    }
                    catch { }
                }

                Thread.Sleep(1500);

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string oldCache = Path.Combine(localAppData, "IconCache.db");
                if (File.Exists(oldCache))
                {
                    try
                    {
                        File.SetAttributes(oldCache, FileAttributes.Normal);
                        File.Delete(oldCache);
                    }
                    catch { }
                }

                string explorerPath = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
                if (Directory.Exists(explorerPath))
                {
                    string[] patterns = { "iconcache_*.db", "thumbcache_*.db" };

                    foreach (var pattern in patterns)
                    {
                        try
                        {
                            var files = Directory.GetFiles(explorerPath, pattern, SearchOption.TopDirectoryOnly);
                            foreach (var file in files)
                            {
                                try
                                {
                                    File.SetAttributes(file, FileAttributes.Normal);
                                    File.Delete(file);
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                Thread.Sleep(500);

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                Thread.Sleep(1000);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                    UseShellExecute = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                });

                Thread.Sleep(2000);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear cache error: {ex.Message}");

                try
                {
                    System.Diagnostics.Process.Start(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")
                    );
                }
                catch { }

                return false;
            }
        }

        //----------------- ICON SELECTION -----------------
        private void ChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isRestore) return;
            e.Handled = true;

            OpenFileDialog dlg = new OpenFileDialog { Filter = $"{GetStr("IconFiles")} (*.ico)|*.ico" };

            if (dlg.ShowDialog() == true)
            {
                icoPath = dlg.FileName;
                try
                {
                    ShowIconFromPath(icoPath);
                    TypeTag.Visibility = Visibility.Collapsed;
                    ToastManager.Success(GetStr("IconLoaded"));
                }
                catch
                {
                    ToastManager.Error(GetStr("InvalidIconFile"));
                }
            }
        }

        private void ShowIconFromPath(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = new IconBitmapDecoder(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var bestFrame = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).FirstOrDefault();
                    if (bestFrame != null)
                    {
                        if (bestFrame.CanFreeze) bestFrame.Freeze();
                        Iconpic.Source = bestFrame;
                    }
                }
                Icon_border.Visibility = Visibility.Collapsed;
                Icon_cross.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        //----------------- RESTORE MODE -----------------
        private void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            isRestore = !isRestore;

            if (isRestore)
            {
                icoPath = null;
                ResetIconPreview();

                RestoreText.SetResourceReference(TextBlock.TextProperty, "NormalMode");
                CustomizeText.SetResourceReference(TextBlock.TextProperty, "RestoreToDefault");

                RestoreIcon.Text = "\uE72C";
                CustomizeIcon.Text = "\uE7A7";

                IconPreviewArea.Cursor = Cursors.Arrow;
                ChooseBtn.IsEnabled = false;
                SlideSub.IsEnabled = true;
                SlidePortable.IsEnabled = false;

                UpdateSubFolderButtonText();
                RefreshSubFoldersState();

                if (DropList.SelectedItem != null) UpdatePreviewFromSelectedFolder();

                ToastManager.Info(GetStr("RestoreModeActive"));
            }
            else
            {
                RestoreText.SetResourceReference(TextBlock.TextProperty, "Restore");
                CustomizeText.SetResourceReference(TextBlock.TextProperty, "ApplyCustomization");

                RestoreIcon.Text = "\uE777";
                CustomizeIcon.Text = "\uE790";

                IconPreviewArea.Cursor = Cursors.Hand;
                ChooseBtn.IsEnabled = true;
                SlideSub.IsEnabled = true;
                SlidePortable.IsEnabled = true;

                UpdateSubFolderButtonText();
                RefreshSubFoldersState();

                if (DropList.SelectedItem != null) UpdatePreviewFromSelectedFolder();
                else ResetIconPreview();

                ToastManager.Success(GetStr("NormalModeActive"));
            }
        }

        private void UpdateSubFolderButtonText()
        {
            if (SubFolderDescriptionText == null) return;

            if (isRestore)
            {
                SubFolderDescriptionText.SetResourceReference(TextBlock.TextProperty, "SubFoldersRestoreMode");
                SlideSub.ToolTip = GetStr("SubFoldersRestoreDesc");

                SubFolderDescriptionText.Foreground = (Brush)FindResource("DynamicWarning");
            }
            else
            {
                SubFolderDescriptionText.SetResourceReference(TextBlock.TextProperty, "SubFoldersIncluded");
                SlideSub.ToolTip = GetStr("SubFoldersIncludedDesc");

                SubFolderDescriptionText.Foreground = (Brush)FindResource("DynamicSubText");
            }
        }

        private void RefreshSubFoldersState()
        {
            if (SlideSub.IsChecked == true)
            {
                AddSubFolders();

                if (isRestore)
                {
                    ToastManager.Warning(string.Format(GetStr("SubFoldersRestoreToast"), subfolderList.Count));
                }
                else
                {
                    ToastManager.Success(string.Format(GetStr("SubFoldersIncludedToast"), subfolderList.Count));
                }
            }
        }

        private void OpenConverter_Click(object sender, RoutedEventArgs e)
            => new IconConverter { Owner = this }.ShowDialog();
        #endregion

        #region================== CUSTOMIZATION ENGINE ==================
        private async void Customize_Click(object sender, RoutedEventArgs e)
        {
            if (folderList.Count == 0)
            {
                ToastManager.Warning(GetStr("ListAlreadyEmpty"));
                return;
            }

            if (icoPath == null && !isRestore)
            {
                ToastManager.Warning(GetStr("ChooseAnIconFirst"));
                return;
            }

            Customize.IsEnabled = false;
            ToastManager.Info(GetStr("ProcessingFolders"));

            List<string> allPaths = new List<string>(folderList);
            if (SlideSub.IsChecked == true) allPaths.AddRange(subfolderList);

            int successCount = 0;
            int failCount = 0;

            await Task.Run(() =>
            {
                foreach (string path in allPaths)
                {
                    try
                    {
                        if (!Directory.Exists(path)) { failCount++; continue; }

                        string iniPath = Path.Combine(path, "desktop.ini");
                        string portIco = Path.Combine(path, "folder_icon.ico");

                        string folderSpecificIco = icoPath;

                        if (folderSpecificIco == null && !isRestore)
                        {
                            if (File.Exists(iniPath))
                            {
                                var lines = File.ReadAllLines(iniPath, Encoding.Default);
                                foreach (var line in lines)
                                {
                                    string cleanLine = line.Trim();
                                    if (cleanLine.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string extracted = cleanLine.Substring("IconResource=".Length).Split(',')[0].Trim();
                                        extracted = extracted.Trim('"', '\'');
                                        folderSpecificIco = Path.IsPathRooted(extracted) ? extracted : Path.Combine(path, extracted);
                                        break;
                                    }
                                }
                            }
                        }

                        if (folderSpecificIco == null && !isRestore) { failCount++; continue; }

                        if (File.Exists(iniPath))
                        {
                            File.SetAttributes(iniPath, FileAttributes.Normal);
                            File.Delete(iniPath);
                        }

                        if (isRestore)
                        {
                            if (File.Exists(portIco)) { File.SetAttributes(portIco, FileAttributes.Normal); File.Delete(portIco); }
                            File.SetAttributes(path, FileAttributes.Normal);
                        }
                        else
                        {
                            string finalIco = folderSpecificIco;
                            if (isPortable)
                            {
                                if (folderSpecificIco != portIco)
                                    File.Copy(folderSpecificIco, portIco, true);

                                File.SetAttributes(portIco, FileAttributes.Hidden | FileAttributes.System);
                                finalIco = "folder_icon.ico";
                            }
                            else
                            {
                                if (File.Exists(portIco) && folderSpecificIco != portIco)
                                {
                                    File.SetAttributes(portIco, FileAttributes.Normal);
                                    File.Delete(portIco);
                                }
                            }

                            var ini = new StringBuilder();
                            ini.AppendLine($"; =============================================");
                            ini.AppendLine($"; {GetStr("AppSignature")}");
                            ini.AppendLine($"; {GetStr("IniComment2")}: {GetStr("DeveloperName")}");
                            ini.AppendLine($"; {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                            ini.AppendLine($"; =============================================");
                            ini.AppendLine("");
                            ini.AppendLine("[.ShellClassInfo]");
                            ini.AppendLine($"IconResource={finalIco},0");
                            File.WriteAllText(iniPath, ini.ToString(), Encoding.Unicode);
                            File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

                            DirectoryInfo dirInfo = new DirectoryInfo(path);
                            dirInfo.Attributes |= FileAttributes.ReadOnly;
                        }

                        IntPtr ptrPath = Marshal.StringToHGlobalUni(path);
                        SHChangeNotify(0x00002000, 0x0005, ptrPath, IntPtr.Zero);
                        Marshal.FreeHGlobal(ptrPath);

                        successCount++;
                    }
                    catch { failCount++; }
                }
            });

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            var tempList = folderList.ToList();
            folderList.Clear();
            foreach (var item in tempList) folderList.Add(item);

            Customize.IsEnabled = true;
            bool wasRestore = isRestore;
            ResetUI();

            if (successCount > 0)
            {
                string msg = wasRestore
                    ? $"{GetStr("FoldersHaveBeenRestored")} ({successCount})"
                    : $"{GetStr("IconsApplied")} ({successCount})";
                ToastManager.Success(msg);
            }

            if (failCount > 0)
                ToastManager.Error($"{GetStr("FailedToProcess")} ({failCount})");
        }
        #endregion

        #region================== PREVIEW & ICON HANDLING ==================
        //----------------- ICON EXTRACTION -----------------
        public static ImageSource GetSafeFolderIcon(string path)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                string iniPath = Path.Combine(path, "desktop.ini");
                if (File.Exists(iniPath))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(iniPath, Encoding.Default);
                        foreach (var line in lines)
                        {
                            string cleanLine = line.Trim();
                            if (cleanLine.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase))
                            {
                                string iconPath = cleanLine.Substring("IconResource=".Length).Split(',')[0].Trim();
                                iconPath = iconPath.Trim('"', '\'');

                                if (!Path.IsPathRooted(iconPath))
                                    iconPath = Path.Combine(path, iconPath);

                                if (File.Exists(iconPath))
                                {
                                    using (FileStream fs = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        IconBitmapDecoder decoder = new IconBitmapDecoder(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                        var bestFrame = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).FirstOrDefault();
                                        if (bestFrame != null)
                                        {
                                            if (bestFrame.CanFreeze) bestFrame.Freeze();
                                            return bestFrame;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    catch { }
                }

                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr res = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_SYSICONINDEX);
                if (res != IntPtr.Zero)
                {
                    IImageList iml;
                    Guid iid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                    if (SHGetImageList(SHIL_JUMBO, ref iid, out iml) == 0)
                    {
                        if (iml.GetIcon(shinfo.iIcon, 0x0001, out hIcon) == 0 && hIcon != IntPtr.Zero)
                            return ConvertIconToBitmap(hIcon);
                    }
                }
            }
            catch { }
            return null;
        }

        private static BitmapSource ConvertIconToBitmap(IntPtr hIcon)
        {
            var img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DestroyIcon(hIcon);
            if (img.CanFreeze) img.Freeze();
            return img;
        }

        //----------------- PREVIEW UPDATE -----------------
        private void UpdatePreviewFromSelectedFolder()
        {
            if (DropList.SelectedItem == null) { ResetIconPreview(); return; }
            string path = DropList.SelectedItem.ToString();
            if (!Directory.Exists(path)) { ResetIconPreview(); return; }

            int subfoldersInsideThisFolder = CountSubfoldersInPath(path);
            Dotsub.Text = subfoldersInsideThisFolder.ToString();

            bool isPortableFolder = false;
            string iniPath = Path.Combine(path, "desktop.ini");

            if (File.Exists(iniPath) && !isRestore)
            {
                try
                {
                    string[] lines = File.ReadAllLines(iniPath, Encoding.Default);
                    foreach (var line in lines)
                    {
                        string cleanLine = line.Trim();
                        if (cleanLine.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase))
                        {
                            string iconVal = cleanLine.Substring("IconResource=".Length).Split(',')[0].Trim();
                            iconVal = iconVal.Trim('"', '\'');
                            isPortableFolder = !Path.IsPathRooted(iconVal);
                            icoPath = Path.IsPathRooted(iconVal) ? iconVal : Path.Combine(path, iconVal);
                            break;
                        }
                    }
                }
                catch { }
            }

            TypeTag.Visibility = Visibility.Visible;
            if (isPortableFolder)
            {
                TypeTagIcon.Text = "\uE88E";
                TypeTagText.Text = GetStr("Portable");
                TypeTag.Background = new SolidColorBrush(Color.FromArgb(30, 14, 165, 233));
                TypeTagText.Foreground = (Brush)FindResource("DynamicAccent");
                TypeTagIcon.Foreground = (Brush)FindResource("DynamicAccent");
                SlidePortable.IsChecked = true;
                isPortable = true;
            }
            else
            {
                TypeTagIcon.Text = "\uE719";
                TypeTagText.Text = GetStr("Normal");
                TypeTag.Background = new SolidColorBrush(Color.FromArgb(30, 139, 92, 246));
                TypeTagText.Foreground = (Brush)FindResource("DynamicPurple");
                TypeTagIcon.Foreground = (Brush)FindResource("DynamicPurple");
                SlidePortable.IsChecked = false;
                isPortable = false;
            }

            var icon = GetSafeFolderIcon(path);
            if (icon != null)
            {
                Iconpic.Source = icon;
                Icon_border.Visibility = Visibility.Collapsed;
                Icon_restore.Visibility = Visibility.Collapsed;
                Icon_cross.Visibility = Visibility.Collapsed;
            }
            else ResetIconPreview();
        }

        private void ResetIconPreview()
        {
            Iconpic.Source = null;
            Icon_border.Visibility = Visibility.Visible;
            Icon_restore.Visibility = isRestore ? Visibility.Visible : Visibility.Collapsed;
            Icon_cross.Visibility = isRestore ? Visibility.Collapsed : Visibility.Visible;
            TypeTag.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region================== DIALOGS & HELPERS ==================
        //----------------- CONFIRMATION DIALOGS -----------------
        private async Task<bool> ShowClearCacheConfirmDialog()
        {
            var tcs = new TaskCompletionSource<bool>();

            Window dialog = new Window
            {
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = false,
                Opacity = this.Opacity,
                FlowDirection = (FlowDirection)Application.Current.Resources["AppFlowDirection"]
            };

            Color accentColor = Application.Current.Resources["DynamicAccent"] is SolidColorBrush ab ? ab.Color : Color.FromRgb(14, 165, 233);
            Brush cardBg = Application.Current.Resources["DynamicCardBg"] as Brush ?? new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Brush borderBr = Application.Current.Resources["DynamicBorderBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Brush mainText = Application.Current.Resources["DynamicMainText"] as Brush ?? Brushes.White;
            Brush subText = Application.Current.Resources["DynamicSubText"] as Brush ?? Brushes.Gray;
            Color bgColor = cardBg is SolidColorBrush scb ? scb.Color : Color.FromRgb(30, 30, 30);

            var bgGradient = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            bgGradient.GradientStops.Add(new GradientStop(accentColor, 0));
            bgGradient.GradientStops.Add(new GradientStop(accentColor, 0.018));
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 0.0181));
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 1));

            var outerBorder = new Border
            {
                Background = bgGradient,
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(1),
                BorderBrush = borderBr,
                ClipToBounds = true
            };

            var stack = new StackPanel { Margin = new Thickness(28, 24, 28, 24) };

            stack.Children.Add(new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                Background = new SolidColorBrush(Color.FromArgb(38, accentColor.R, accentColor.G, accentColor.B)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = "\uE7BA",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 26,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            stack.Children.Add(new TextBlock
            {
                Text = GetStr("ClearCache"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = mainText,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, accentColor.R, accentColor.G, accentColor.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 26),
                Child = new TextBlock
                {
                    Text = GetStr("ClearCacheWarning"),
                    FontSize = 12,
                    Foreground = subText,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    LineHeight = 18
                }
            });

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesBtn = CreateStyledButton(GetStr("Yes"), accentColor, Brushes.White, true);
            yesBtn.Margin = new Thickness(0, 0, 8, 0);
            yesBtn.Click += (_, __) => { tcs.SetResult(true); dialog.Close(); };

            var noBtn = CreateStyledButton(GetStr("No"), System.Windows.Media.Colors.Transparent, mainText, false, borderBr);
            noBtn.Click += (_, __) => { tcs.SetResult(false); dialog.Close(); };

            buttonStack.Children.Add(yesBtn);
            buttonStack.Children.Add(noBtn);
            stack.Children.Add(buttonStack);

            outerBorder.Child = stack;
            dialog.Content = outerBorder;

            dialog.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Escape)
                {
                    tcs.SetResult(false);
                    dialog.Close();
                }
            };

            dialog.MouseLeftButtonDown += (s, ev) =>
            {
                if (ev.ButtonState == MouseButtonState.Pressed)
                    dialog.DragMove();
            };

            dialog.Loaded += (_, __) =>
            {
                dialog.Opacity = 0;
                var anim = new DoubleAnimation(0, this.Opacity, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                dialog.BeginAnimation(Window.OpacityProperty, anim);
            };

            dialog.ShowDialog();
            return await tcs.Task;
        }

        //----------------- BUTTON STYLES -----------------
        private Button CreateStyledButton(string text, Color bgColor, Brush fgColor, bool isFilled, Brush borderBrush = null)
        {
            var btn = new Button
            {
                Content = text,
                Width = 110,
                Height = 38,
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = fgColor
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));

            if (isFilled)
            {
                border.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
                border.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            }
            else
            {
                border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                border.SetValue(Border.BorderBrushProperty, borderBrush);
                border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            }

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);
            template.VisualTree = border;
            btn.Template = template;

            btn.MouseEnter += (_, __) => btn.Opacity = 0.85;
            btn.MouseLeave += (_, __) => btn.Opacity = 1.0;

            return btn;
        }

        //----------------- HELP DIALOGS -----------------
        private void Help_Click(object sender, RoutedEventArgs e) => ShowPortableHelpDialog();

        private void ShowPortableHelpDialog()
        {
            Window dialog = new Window
            {
                Width = 380,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = false,
                Opacity = this.Opacity,
                FlowDirection = (FlowDirection)Application.Current.Resources["AppFlowDirection"]
            };

            Color accentColor = Application.Current.Resources["DynamicAccent"] is SolidColorBrush ab ? ab.Color : Color.FromRgb(14, 165, 233);
            Brush cardBg = Application.Current.Resources["DynamicCardBg"] as Brush ?? new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Brush borderBr = Application.Current.Resources["DynamicBorderBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Brush mainText = Application.Current.Resources["DynamicMainText"] as Brush ?? Brushes.White;
            Brush subText = Application.Current.Resources["DynamicSubText"] as Brush ?? Brushes.Gray;
            Color bgColor = cardBg is SolidColorBrush scb ? scb.Color : Color.FromRgb(30, 30, 30);

            var bgGradient = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            bgGradient.GradientStops.Add(new GradientStop(accentColor, 0));
            bgGradient.GradientStops.Add(new GradientStop(accentColor, 0.018));
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 0.0181));
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 1));

            var outerBorder = new Border { Background = bgGradient, CornerRadius = new CornerRadius(16), BorderThickness = new Thickness(1), BorderBrush = borderBr, ClipToBounds = true };
            var stack = new StackPanel { Margin = new Thickness(28, 24, 28, 24) };

            stack.Children.Add(new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                Background = new SolidColorBrush(Color.FromArgb(38, accentColor.R, accentColor.G, accentColor.B)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = "\uE946",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 26,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            stack.Children.Add(new TextBlock
            {
                Text = GetStr("PortableHelpTitle"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = mainText,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, accentColor.R, accentColor.G, accentColor.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 26),
                Child = new TextBlock
                {
                    Text = GetStr("PortableHelpDesc"),
                    FontSize = 12,
                    Foreground = subText,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    LineHeight = 18
                }
            });

            var okBtn = CreateStyledButton(GetStr("OK"), accentColor, Brushes.White, true);
            okBtn.Width = 120;
            okBtn.HorizontalAlignment = HorizontalAlignment.Center;
            okBtn.Click += (_, __) => { dialog.DialogResult = true; dialog.Close(); };
            stack.Children.Add(okBtn);

            outerBorder.Child = stack;
            dialog.Content = outerBorder;

            dialog.KeyDown += (s, ev) => { if (ev.Key == Key.Escape) { dialog.DialogResult = true; dialog.Close(); } };
            dialog.MouseLeftButtonDown += (s, ev) => { if (ev.ButtonState == MouseButtonState.Pressed) dialog.DragMove(); };

            dialog.Loaded += (_, __) =>
            {
                dialog.Opacity = 0;
                var anim = new DoubleAnimation(0, this.Opacity, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                dialog.BeginAnimation(Window.OpacityProperty, anim);
            };

            dialog.ShowDialog();
        }
        #endregion

        #region================== SUBFOLDERS EVENTS ==================
        private async void SlideSub_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSubFolders) return;
            _isUpdatingSubFolders = true;

            try
            {
                await Task.Delay(50);

                AddSubFolders();

                if (folderList.Count > 0)
                {
                    if (isRestore)
                    {
                        ToastManager.Warning(string.Format(GetStr("SubFoldersRestoreToast"), subfolderList.Count));
                    }
                    else
                    {
                        ToastManager.Success(string.Format(GetStr("SubFoldersIncludedToast"), subfolderList.Count));
                    }
                }
            }
            finally
            {
                _isUpdatingSubFolders = false;
            }
        }

        private void SlideSub_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSubFolders) return;
            _isUpdatingSubFolders = true;

            try
            {
                int previousCount = subfolderList.Count;
                subfolderList.Clear();
                NCount();

                if (previousCount > 0 && folderList.Count > 0)
                {
                    ToastManager.Info(GetStr("SubFoldersExcluded"));
                }
            }
            finally
            {
                _isUpdatingSubFolders = false;
            }
        }

        private void SlidePortable_Click(object sender, RoutedEventArgs e)
        {
            isPortable = SlidePortable.IsChecked == true;
            ToastManager.Info(isPortable ? GetStr("PortableModeOn") : GetStr("PortableModeOff"));
        }

        private void IconPreviewArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isRestore)
            {
                e.Handled = true;
                ChooseBtn_Click(sender, e);
            }
        }
        #endregion

        #region================== DRAG & DROP ==================
        private void DropList_DragEnter(object sender, DragEventArgs e)
            => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        private void DropList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                int addedCount = 0;
                int skippedCount = 0;

                foreach (string f in files)
                {
                    if (Directory.Exists(f))
                    {
                        if (!folderList.Contains(f))
                        {
                            folderList.Add(f);
                            addedCount++;
                        }
                        else skippedCount++;
                    }
                    else skippedCount++;
                }

                if (SlideSub.IsChecked == true) AddSubFolders();
                NCount();

                if (addedCount > 0)
                    ToastManager.Success($"{GetStr("FoldersAdded")} ({addedCount})");

                if (skippedCount > 0)
                    ToastManager.Warning($"{GetStr("ItemsSkipped")} ({skippedCount})");
            }
        }

        private void DropList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DropList.SelectedItem != null)
            {
                UpdatePreviewFromSelectedFolder();
            }
            else
            {
                if (SlideSub.IsChecked == true)
                    Dotsub.Text = subfolderList.Count.ToString();
                else
                    Dotsub.Text = "0";

                if (icoPath != null && !isRestore) ShowIconFromPath(icoPath);
                else ResetIconPreview();
            }
        }
        #endregion
    }

    #region================== VALUE CONVERTER ==================
    public class PathToIconConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string path && Directory.Exists(path)) return MainWindow.GetSafeFolderIcon(path);
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
    #endregion
}