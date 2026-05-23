using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CrystalFolders.Tray
{
    public partial class TrayMenu : Window
    {
        #region================== WIN32 API ==================
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        #endregion

        #region================== INITIALIZATION ==================
        public TrayMenu()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;

            try
            {
                var mw = Application.Current.MainWindow;
                if (mw != null && mw.IsVisible)
                    this.Owner = mw;
            }
            catch { }

            this.SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                exStyle = (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
            }
            catch { }
        }
        #endregion

        #region================== WINDOW EVENTS ==================
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.IsLoaded) this.Close();
            }), DispatcherPriority.Background);
        }
        #endregion

        #region================== MENU ACTIONS ==================
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            App.TrayManager?.TriggerAction("open");
            this.Close();
        }

        private void Converter_Click(object sender, RoutedEventArgs e)
        {
            App.TrayManager?.TriggerAction("converter");
            this.Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            App.TrayManager?.TriggerAction("settings");
            this.Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            App.TrayManager?.TriggerAction("about");
            this.Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            App.TrayManager?.TriggerAction("exit");
        }
        #endregion
    }
}