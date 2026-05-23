using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using CrystalFolders.Helpers;

using WPFApp = System.Windows.Application;

namespace CrystalFolders.Tray
{
    public class TrayManager : IDisposable
    {
        #region================== FIELDS ==================
        private NotifyIcon _notifyIcon;
        private TrayMenu _menuWindow;
        private bool _isDisposed;

        public bool IsEnabled { get; private set; }
        #endregion

        #region================== INITIALIZATION ==================
        public TrayManager()
        {
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();

                try
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    _notifyIcon.Icon = Icon.ExtractAssociatedIcon(exePath);
                }
                catch
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }

                _notifyIcon.Text = GetString("AppTitle", "Crystal Folders");
                _notifyIcon.Visible = false;

                _notifyIcon.MouseClick += OnTrayIconMouseClick;
                _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tray Init Error] {ex.Message}");
            }
        }
        #endregion

        #region================== EVENT HANDLERS ==================
        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                ShowTrayMenu();
            else if (e.Button == MouseButtons.Left)
                ShowMainWindow();
        }
        #endregion

        #region================== MENU DISPLAY ==================
        private void ShowTrayMenu()
        {
            try
            {
                if (_menuWindow != null)
                {
                    try { _menuWindow.Close(); } catch { }
                    _menuWindow = null;
                }

                _menuWindow = new TrayMenu();

                _menuWindow.ShowInTaskbar = false;
                if (WPFApp.Current.MainWindow != null)
                    _menuWindow.Owner = WPFApp.Current.MainWindow;

                var mouse = System.Windows.Forms.Control.MousePosition;

                var source = WPFApp.Current.MainWindow != null
                    ? PresentationSource.FromVisual(WPFApp.Current.MainWindow)
                    : null;

                if (source?.CompositionTarget != null)
                {
                    var transform = source.CompositionTarget.TransformFromDevice;
                    var wpfPoint = transform.Transform(new System.Windows.Point(mouse.X, mouse.Y));

                    _menuWindow.Left = wpfPoint.X - 175;
                    _menuWindow.Top = wpfPoint.Y - 290;
                }
                else
                {
                    _menuWindow.Left = mouse.X - 175;
                    _menuWindow.Top = mouse.Y - 290;
                }

                if (_menuWindow.Top < 0) _menuWindow.Top = 10;
                if (_menuWindow.Left < 0) _menuWindow.Left = 10;

                _menuWindow.Show();
                _menuWindow.Activate();
                _menuWindow.Topmost = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Show Menu Error] {ex.Message}");
            }
        }
        #endregion

        #region================== ACTIONS ==================
        public void TriggerAction(string action)
        {
            WPFApp.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    switch (action)
                    {
                        case "open":
                            ShowMainWindow();
                            break;

                        case "converter":
                            OpenConverter();
                            break;

                        case "settings":
                            OpenSettings();
                            break;

                        case "about":
                            OpenAbout();
                            break;

                        case "exit":
                            ExitApplication();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Trigger Action Error] {ex.Message}");
                }
            });
        }

        //----------------- WINDOW OPENERS -----------------
        private void ShowMainWindow()
        {
            App.ShowMainWindow();
        }

        private void OpenConverter()
        {
            try
            {
                var converter = new IconConverter
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = true
                };
                if (WPFApp.Current.MainWindow != null && WPFApp.Current.MainWindow.IsVisible)
                    converter.Owner = WPFApp.Current.MainWindow;

                converter.ShowDialog();
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetString("Error", "Error")}: {ex.Message}");
            }
        }

        private void OpenSettings()
        {
            try
            {
                var settings = new Settings
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = true
                };
                if (WPFApp.Current.MainWindow != null && WPFApp.Current.MainWindow.IsVisible)
                    settings.Owner = WPFApp.Current.MainWindow;

                settings.ShowDialog();
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetString("Error", "Error")}: {ex.Message}");
            }
        }

        private void OpenAbout()
        {
            try
            {
                var about = new About
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = true
                };
                if (WPFApp.Current.MainWindow != null && WPFApp.Current.MainWindow.IsVisible)
                    about.Owner = WPFApp.Current.MainWindow;

                about.ShowDialog();
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetString("Error", "Error")}: {ex.Message}");
            }
        }

        private void ExitApplication()
        {
            Dispose();
            WPFApp.Current.Shutdown();
        }
        #endregion

        #region================== PUBLIC METHODS ==================
        public void SetVisible(bool isVisible)
        {
            IsEnabled = isVisible;
            if (_notifyIcon != null) _notifyIcon.Visible = isVisible;
        }

        public void UpdateLanguage()
        {
            if (_notifyIcon != null)
                _notifyIcon.Text = GetString("AppTitle", "Crystal Folders");
        }

        private string GetString(string key, string fallback)
        {
            try { return WPFApp.Current.TryFindResource(key) as string ?? fallback; }
            catch { return fallback; }
        }
        #endregion

        #region================== DISPOSAL ==================
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                if (_menuWindow != null) { _menuWindow.Close(); _menuWindow = null; }
            }
            catch { }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
        #endregion
    }
}