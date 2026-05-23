using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CrystalFolders.Helpers;

namespace CrystalFolders
{
    public partial class About : Window
    {
        #region================== INITIALIZATION ==================
        public About()
        {
            InitializeComponent();
            ApplyOwnerOpacity();
        }

        //----------------- OPACITY -----------------
        private void ApplyOwnerOpacity()
        {
            try
            {
                double ownerOpacity = 1.0;

                if (Owner != null)
                    ownerOpacity = Owner.Opacity;
                else if (Application.Current?.MainWindow != null)
                    ownerOpacity = Application.Current.MainWindow.Opacity;

                this.Opacity = 0;
                this.Loaded += (_, __) =>
                {
                    var anim = new DoubleAnimation(0, ownerOpacity, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    this.BeginAnimation(Window.OpacityProperty, anim);
                };
            }
            catch
            {
                this.Opacity = 1.0;
            }
        }
        #endregion

        #region================== WINDOW CONTROLS ==================
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        #endregion

        #region================== LINKS ==================
        private void btnWebsite_Click(object sender, RoutedEventArgs e) => OpenLink("https://github.com/Diagoo1");
        private void btnPayPal_Click(object sender, RoutedEventArgs e) => OpenLink("https://paypal.me/Diagoo1");
        private void btnEmail_Click(object sender, RoutedEventArgs e) => OpenLink("mailto:tarek.sadek44@gmail.com");

        private void OpenLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                string msg = Application.Current.TryFindResource("CouldNotOpenLink")?.ToString()
                             ?? "Could not open link";
                ToastManager.Error($"{msg}: {ex.Message}");
            }
        }
        #endregion
    }
}