using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Input;
using Microsoft.Win32;
using CrystalFolders.Themes;
using CrystalFolders.Tray;
using CrystalFolders.Helpers;

using MW = CrystalFolders.MainWindow;

namespace CrystalFolders
{
    public partial class App : Application
    {
        #region================== FIELDS & PROPERTIES ==================
        public static TrayManager TrayManager { get; private set; }
        public static bool IsTrayEnabled { get; private set; }

        private static Mutex _singleInstanceMutex;
        private const string MUTEX_NAME = "CrystalFolders_SingleInstance_Mutex_8F3A2B1C";
        private const string REG_PATH = @"SOFTWARE\CrystalFolders";

        private static MainWindow _mainWindow;
        #endregion

        #region================== APPLICATION STARTUP ==================
        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, MUTEX_NAME, out createdNew);

            string savedLang = ReadLanguageFromRegistry();

            if (!createdNew)
            {
                if (e.Args.Length > 0)
                {
                    bool sent = PipeServer.SendToRunningInstance(e.Args);
                    if (!sent)
                    {
                        ThemeManager.ApplyLanguage(savedLang);
                        ShowAlreadyRunningDialog(savedLang);
                    }
                }
                else
                {
                    ThemeManager.ApplyLanguage(savedLang);
                    ShowAlreadyRunningDialog(savedLang);
                }
                Shutdown();
                return;
            }

            base.OnStartup(e);

            ThemeManager.Initialize();
            ThemeManager.ApplyLanguage(savedLang);

            ContextMenuHelper.AutoSyncRegistryOnStartup();

            double savedOpacity = LoadSavedOpacity();
            LoadTrayEnabled();

            TrayManager = new TrayManager();
            TrayManager.SetVisible(IsTrayEnabled);

            PipeServer.MessageReceived += OnPipeMessageReceived;
            PipeServer.Start();

            if (e.Args.Length > 0)
            {
                HandleCommandLineArgs(e.Args, savedOpacity);
                return;
            }

            CreateAndShowMainWindow(savedOpacity);
        }

        //----------------- LOAD SAVED SETTINGS -----------------
        private double LoadSavedOpacity()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REG_PATH))
                {
                    if (key != null)
                    {
                        double opacityPercent = Convert.ToDouble(key.GetValue("Opacity") ?? 100);
                        return Math.Max(0.3, Math.Min(1.0, opacityPercent / 100.0));
                    }
                }
            }
            catch { }
            return 1.0;
        }

        private void LoadTrayEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REG_PATH))
                {
                    IsTrayEnabled = Convert.ToBoolean(key?.GetValue("TrayEnabled") ?? false);
                }
            }
            catch { IsTrayEnabled = false; }
        }

        //----------------- CREATE WINDOWS -----------------
        private void CreateAndShowMainWindow(double opacity)
        {
            _mainWindow = new MainWindow();
            Current.MainWindow = _mainWindow;
            _mainWindow.Opacity = opacity;
            _mainWindow.StateChanged += OnMainWindowStateChanged;
            _mainWindow.Show();
        }
        #endregion

        #region================== COMMAND LINE HANDLING ==================
        private void HandleCommandLineArgs(string[] args, double opacity)
        {
            try
            {
                if (args.Length >= 2)
                {
                    string command = args[0].ToLower();
                    string path = args[1];

                    if (command == "--folder")
                    {
                        HandleFolderCommand(args, opacity);
                    }
                    else if (command == "--convert")
                    {
                        OpenConverterStandalone(path, opacity);
                    }
                }
                else if (args.Length == 1)
                {
                    HandleSinglePathArgument(args[0], opacity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CommandLine Error] {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Crystal Folders",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void HandleFolderCommand(string[] args, double opacity)
        {
            _mainWindow = new MainWindow();
            Current.MainWindow = _mainWindow;
            _mainWindow.Opacity = opacity;
            _mainWindow.StateChanged += OnMainWindowStateChanged;

            for (int i = 1; i < args.Length; i++)
            {
                if (Directory.Exists(args[i]))
                    MW.folderList.Add(args[i]);
            }

            TrayManager.SetVisible(IsTrayEnabled);
            _mainWindow.Show();
        }

        private void HandleSinglePathArgument(string path, double opacity)
        {
            if (Directory.Exists(path))
            {
                _mainWindow = new MainWindow();
                Current.MainWindow = _mainWindow;
                _mainWindow.Opacity = opacity;
                _mainWindow.StateChanged += OnMainWindowStateChanged;

                MW.folderList.Add(path);

                TrayManager.SetVisible(IsTrayEnabled);
                _mainWindow.Show();
            }
            else if (File.Exists(path))
            {
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    OpenConverterStandalone(path, opacity);
                }
                else
                {
                    CreateAndShowMainWindow(opacity);
                }
            }
        }

        private void OpenConverterStandalone(string imagePath, double opacity)
        {
            try
            {
                var converter = new IconConverter();
                Current.MainWindow = converter;
                converter.Opacity = opacity;
                converter.LoadImageFromPath(imagePath);

                converter.Closed += (_, __) => Shutdown();
                converter.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Converter Error: {ex.Message}", "Crystal Folders",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
        #endregion

        #region================== PIPE COMMUNICATION ==================
        private void OnPipeMessageReceived(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0) return;

                EnsureMainWindowExists();

                if (args.Length >= 2)
                {
                    string command = args[0].ToLower();
                    string path = args[1];

                    if (command == "--folder")
                    {
                        HandlePipeFolderCommand(args);
                    }
                    else if (command == "--convert")
                    {
                        HandlePipeConvertCommand(path);
                    }
                }
                else if (args.Length == 1)
                {
                    HandlePipeSinglePath(args[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnPipeMessageReceived] {ex.Message}");
            }
        }

        private void EnsureMainWindowExists()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                Current.MainWindow = _mainWindow;
                _mainWindow.StateChanged += OnMainWindowStateChanged;
            }
        }

        private void HandlePipeFolderCommand(string[] args)
        {
            ShowMainWindow();

            for (int i = 1; i < args.Length; i++)
            {
                if (Directory.Exists(args[i]) && !MW.folderList.Contains(args[i]))
                    MW.folderList.Add(args[i]);
            }

            ToastManager.Success($"{GetStr("FoldersAdded")} ({args.Length - 1})");
        }

        private void HandlePipeConvertCommand(string path)
        {
            if (!_mainWindow.IsVisible)
                _mainWindow.Show();

            var converter = new IconConverter { Owner = _mainWindow };
            converter.LoadImageFromPath(path);
            converter.Show();
        }

        private void HandlePipeSinglePath(string path)
        {
            if (Directory.Exists(path))
            {
                ShowMainWindow();
                if (!MW.folderList.Contains(path))
                    MW.folderList.Add(path);

                ToastManager.Success($"{GetStr("FoldersAdded")} (1)");
            }
            else if (File.Exists(path))
            {
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    if (!_mainWindow.IsVisible)
                        _mainWindow.Show();

                    var converter = new IconConverter { Owner = _mainWindow };
                    converter.LoadImageFromPath(path);
                    converter.Show();
                }
            }
        }
        #endregion

        #region================== REGISTRY HELPERS ==================
        private static string ReadLanguageFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REG_PATH))
                {
                    var val = key?.GetValue("Language")?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val.ToLower();
                }
            }
            catch { }
            return "en";
        }
        #endregion

        #region================== UTILITIES ==================
        private static string GetStr(string key, string fallback = null)
        {
            try
            {
                var val = Application.Current?.TryFindResource(key)?.ToString();
                if (!string.IsNullOrEmpty(val)) return val;
            }
            catch { }
            return fallback ?? key;
        }

        public static void ApplyWindowOpacity(Window window)
        {
            try
            {
                double targetOpacity = window.Owner?.Opacity
                    ?? Current?.MainWindow?.Opacity
                    ?? 1.0;

                window.Opacity = 0;
                window.Loaded += (_, __) =>
                {
                    var anim = new DoubleAnimation(0, targetOpacity, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new QuadraticEase
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                    };
                    window.BeginAnimation(Window.OpacityProperty, anim);
                };
            }
            catch { window.Opacity = 1.0; }
        }

        private static void OnMainWindowStateChanged(object sender, EventArgs e)
        {
            if (_mainWindow.WindowState == WindowState.Minimized && IsTrayEnabled)
            {
                _mainWindow.ShowInTaskbar = false;
                _mainWindow.Hide();
            }
        }

        public static void ShowMainWindow()
        {
            if (_mainWindow == null) return;
            _mainWindow.ShowInTaskbar = true;
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Focus();
        }

        public static void SetTrayEnabled(bool enabled)
        {
            IsTrayEnabled = enabled;
            TrayManager?.SetVisible(enabled);

            if (_mainWindow == null) return;

            if (enabled)
            {
                _mainWindow.StateChanged -= OnMainWindowStateChanged;
                _mainWindow.StateChanged += OnMainWindowStateChanged;
            }
            else
            {
                _mainWindow.StateChanged -= OnMainWindowStateChanged;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.Show();
                if (_mainWindow.WindowState == WindowState.Minimized)
                    _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REG_PATH))
                {
                    key.SetValue("TrayEnabled", enabled);
                }
            }
            catch { }
        }
        #endregion

        #region================== APPLICATION EXIT ==================
        protected override void OnExit(ExitEventArgs e)
        {
            PipeServer.Stop();
            TrayManager?.Dispose();
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
            }
            catch { }
            base.OnExit(e);
        }
        #endregion

        #region================== DIALOGS ==================
        private static void ShowAlreadyRunningDialog(string langCode)
        {
            bool isRtl = langCode == "ar";

            string title = GetStr("AlreadyRunningTitle", "Already Running");
            string line1 = GetStr("AlreadyRunningLine1", "Crystal Folders is already running");
            string line2 = GetStr("AlreadyRunningLine2", "Check the system tray icon");
            string btnText = GetStr("OK", "OK");
            var flowDir = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            System.Windows.Media.Color accentColor = System.Windows.Media.Color.FromRgb(14, 165, 233);
            System.Windows.Media.Color bgColor = System.Windows.Media.Color.FromRgb(32, 32, 32);
            System.Windows.Media.Color borderColor = System.Windows.Media.Color.FromRgb(64, 64, 64);
            System.Windows.Media.Color textColor = System.Windows.Media.Color.FromRgb(255, 255, 255);
            System.Windows.Media.Color subColor = System.Windows.Media.Color.FromRgb(160, 160, 160);

            var dialog = new Window
            {
                Width = 360,
                SizeToContent = SizeToContent.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true,
                Topmost = true,
                FlowDirection = flowDir
            };

            var bgGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };
            bgGradient.GradientStops.Add(new GradientStop(accentColor, 0.00));
            bgGradient.GradientStops.Add(new GradientStop(accentColor, 0.015));
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 0.016));
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 1.00));

            var outerBorder = new Border
            {
                Background = bgGradient,
                CornerRadius = new CornerRadius(18),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(borderColor),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 30,
                    ShadowDepth = 0,
                    Opacity = 0.45,
                    Color = System.Windows.Media.Colors.Black
                }
            };

            var stack = new StackPanel { Margin = new Thickness(30, 26, 30, 26) };

            stack.Children.Add(new Border
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(30),
                Background = new SolidColorBrush(Color.FromArgb(35, accentColor.R, accentColor.G, accentColor.B)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14),
                Child = new TextBlock
                {
                    Text = "\uE8BD",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 26,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var msgCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, accentColor.R, accentColor.G, accentColor.B)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 24),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1)
            };

            var msgStack = new StackPanel();
            msgStack.Children.Add(new TextBlock
            {
                Text = line1,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(textColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
            msgStack.Children.Add(new TextBlock
            {
                Text = line2,
                FontSize = 11,
                Foreground = new SolidColorBrush(subColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            });
            msgCard.Child = msgStack;
            stack.Children.Add(msgCard);

            var okBtn = new Button
            {
                Content = btnText,
                Width = 130,
                Height = 40,
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnTemplate = new ControlTemplate(typeof(Button));
            var btnBorder = new FrameworkElementFactory(typeof(Border));
            btnBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            btnBorder.SetValue(Border.BackgroundProperty,
                new LinearGradientBrush(accentColor,
                    Color.FromRgb(2, 132, 199),
                    new Point(0, 0),
                    new Point(1, 1)));
            var btnCp = new FrameworkElementFactory(typeof(ContentPresenter));
            btnCp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnCp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            btnBorder.AppendChild(btnCp);
            btnTemplate.VisualTree = btnBorder;
            okBtn.Template = btnTemplate;

            okBtn.MouseEnter += (_, __) => okBtn.Opacity = 0.85;
            okBtn.MouseLeave += (_, __) => okBtn.Opacity = 1.0;
            okBtn.Click += (_, __) => { dialog.DialogResult = true; dialog.Close(); };
            stack.Children.Add(okBtn);

            outerBorder.Child = stack;
            dialog.Content = outerBorder;

            dialog.MouseLeftButtonDown += (s, ev) =>
            {
                if (ev.ButtonState == MouseButtonState.Pressed) dialog.DragMove();
            };

            dialog.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Escape || ev.Key == Key.Return)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };

            dialog.Loaded += (_, __) =>
            {
                dialog.Opacity = 0;
                var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                dialog.BeginAnimation(Window.OpacityProperty, anim);
            };

            dialog.ShowDialog();
        }
        #endregion
    }
}