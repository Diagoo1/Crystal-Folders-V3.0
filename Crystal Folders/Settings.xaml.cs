using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;
using CrystalFolders.Themes;
using CrystalFolders.Helpers;

namespace CrystalFolders
{
    public partial class Settings : Window
    {
        #region================== FIELDS & CONSTANTS ==================
        private const string REG_PATH = @"SOFTWARE\CrystalFolders";
        #endregion

        #region================== INITIALIZATION ==================
        public Settings()
        {
            InitializeComponent();
            App.ApplyWindowOpacity(this);
            Loaded += Settings_Loaded;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e) => LoadSettings();
        #endregion

        #region================== LOAD SETTINGS ==================
        private void LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REG_PATH))
                {
                    string lang = key?.GetValue("Language")?.ToString()?.ToLower() ?? "en";
                    SelectLanguageInCombo(lang);

                    chkDarkMode.IsChecked = ThemeManager.IsDarkMode;

                    double opacityPercent = Convert.ToDouble(key?.GetValue("Opacity") ?? 100);
                    sldOpacity.Value = opacityPercent;
                    ApplyGlobalOpacity(opacityPercent / 100.0);

                    chkTray.IsChecked = Convert.ToBoolean(key?.GetValue("TrayEnabled") ?? false);
                    chkContextMenu.IsChecked = IsContextMenuEnabled();
                }

                using (var runKey = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                {
                    chkStartup.IsChecked = runKey?.GetValue("CrystalFolders") != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSettings Error: {ex.Message}");
            }
        }

        private void SelectLanguageInCombo(string langCode)
        {
            foreach (var obj in cmbLanguage.Items)
            {
                if (obj is ComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), langCode, StringComparison.OrdinalIgnoreCase))
                {
                    cmbLanguage.SelectedItem = item;
                    return;
                }
            }
            cmbLanguage.SelectedIndex = 0;
        }

        private string GetSelectedLangCode()
        {
            if (cmbLanguage.SelectedItem is ComboBoxItem item)
                return item.Tag?.ToString()?.ToLower() ?? "en";
            return "en";
        }

        private void ApplyGlobalOpacity(double opacity)
        {
            opacity = Math.Max(0.3, Math.Min(1.0, opacity));
            this.Opacity = opacity;
            foreach (Window window in Application.Current.Windows)
                window.Opacity = opacity;
        }
        #endregion

        #region================== UTILITIES ==================
        private string GetStr(string key)
            => Application.Current.TryFindResource(key)?.ToString() ?? key;
        #endregion

        #region================== SAVE SETTINGS ==================
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string langCode = GetSelectedLangCode();
                bool isDark = chkDarkMode.IsChecked ?? false;
                bool trayEnabled = chkTray.IsChecked ?? false;
                bool contextMenuEnabled = chkContextMenu.IsChecked ?? false;

                using (var key = Registry.CurrentUser.CreateSubKey(REG_PATH))
                {
                    key.SetValue("Language", langCode);
                    key.SetValue("Opacity", sldOpacity.Value);

                    using (var runKey = Registry.CurrentUser.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (chkStartup.IsChecked == true)
                            runKey?.SetValue("CrystalFolders",
                                Assembly.GetExecutingAssembly().Location);
                        else
                            runKey?.DeleteValue("CrystalFolders", false);
                    }
                }

                if (contextMenuEnabled)
                    EnableContextMenu();
                else
                    DisableContextMenu();

                ThemeManager.ApplyLanguage(langCode);
                ToastManager.RefreshFlowDirection();

                if (isDark != ThemeManager.IsDarkMode)
                    ThemeManager.ToggleTheme();

                App.SetTrayEnabled(trayEnabled);
                App.TrayManager?.UpdateLanguage();

                string savedMsg = GetStr("SettingsSaved");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ToastManager.Success(savedMsg);
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);

                this.Close();
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetStr("Error")}: {ex.Message}");
            }
        }
        #endregion

        #region================== CONTEXT MENU ==================
        private bool IsContextMenuEnabled()
        {
            return ContextMenuHelper.IsContextMenuEnabled();
        }

        private void EnableContextMenu()
        {
            if (ContextMenuHelper.UpdateRegistryPaths())
                ToastManager.Success(GetStr("ContextMenuEnabled"));
            else
                ToastManager.Error(GetStr("NeedAdminRights"));
        }

        private void DisableContextMenu()
        {
            if (ContextMenuHelper.RemoveContextMenu())
                ToastManager.Info(GetStr("ContextMenuDisabled"));
            else
                ToastManager.Error(GetStr("ContextMenuError"));
        }

        // ================== CONTEXT MENU HELP DIALOG ==================
        private void btnContextMenuHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowContextMenuHelpDialog();
        }

        private void ShowContextMenuHelpDialog()
        {
            Window dialog = new Window
            {
                Width = 420,
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

            // Icon
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
                    Text = "\uE8A7",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 26,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            // Title
            stack.Children.Add(new TextBlock
            {
                Text = GetStr("ContextMenuHelpTitle"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = mainText,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Description
            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, accentColor.R, accentColor.G, accentColor.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 16),
                Child = new TextBlock
                {
                    Text = GetStr("ContextMenuHelpDesc"),
                    FontSize = 12,
                    Foreground = subText,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    LineHeight = 18
                }
            });

            // Features Grid
            var featuresGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Folder Feature
            var folderFeature = CreateFeatureItem("\uE8B7", GetStr("ContextMenuFolders"), GetStr("ContextMenuFoldersDesc"), accentColor);
            Grid.SetColumn(folderFeature, 0);
            featuresGrid.Children.Add(folderFeature);

            // Image Feature
            var imageFeature = CreateFeatureItem("\uE7D8", GetStr("ContextMenuImages"), GetStr("ContextMenuImagesDesc"), accentColor);
            Grid.SetColumn(imageFeature, 1);
            featuresGrid.Children.Add(imageFeature);

            stack.Children.Add(featuresGrid);

            // Note Box
            var noteBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, accentColor.R, accentColor.G, accentColor.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 20),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };

            var noteStack = (StackPanel)noteBox.Child;
            noteStack.Children.Add(new TextBlock
            {
                Text = "\uE785",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = new SolidColorBrush(accentColor),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            noteStack.Children.Add(new TextBlock
            {
                Text = GetStr("ContextMenuNote"),
                FontSize = 11,
                Foreground = subText,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300
            });

            stack.Children.Add(noteBox);

            // OK Button
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

        private Border CreateFeatureItem(string icon, string title, string description, Color accentColor)
        {
            var border = new Border
            {
                Margin = new Thickness(4),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(10, accentColor.R, accentColor.G, accentColor.B))
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                Foreground = new SolidColorBrush(accentColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            stack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            });

            border.Child = stack;
            return border;
        }
        #endregion

        #region================== UI EVENTS ==================
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void ToggleTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            chkDarkMode.IsChecked = ThemeManager.IsDarkMode;
        }

        private void sldOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded) ApplyGlobalOpacity(e.NewValue / 100.0);
        }

        private void btnReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/Diagoo1",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetStr("CouldNotOpenBrowser")}: {ex.Message}");
            }
        }

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
        #endregion
    }
}