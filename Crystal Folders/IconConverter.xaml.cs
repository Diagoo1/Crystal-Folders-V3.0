using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CrystalFolders.Helpers;

namespace CrystalFolders
{
    public partial class IconConverter : Window
    {
        #region================== FIELDS ==================
        private string selectedImagePath;
        private DispatcherTimer clickTimer;
        #endregion

        #region================== INITIALIZATION ==================
        public IconConverter()
        {
            InitializeComponent();
            ApplyOwnerOpacity();

            clickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            clickTimer.Tick += SingleClickTimer_Tick;
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

        //----------------- LOAD IMAGE FROM PATH -----------------
        public void LoadImageFromPath(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    selectedImagePath = imagePath;
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(imagePath);
                    bmp.EndInit();
                    ImgPreview.Source = bmp;
                    PlaceholderText.Visibility = Visibility.Collapsed;

                    string dir = Path.GetDirectoryName(imagePath);
                    string name = Path.GetFileNameWithoutExtension(imagePath);
                    UpdateSavePathText(Path.Combine(dir, name + ".ico"));

                    ToastManager.Success(GetStr("ImageLoaded"));
                }
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetStr("ImageLoadFailed")}: {ex.Message}");
            }
        }
        #endregion

        #region================== UTILITIES ==================
        private string GetStr(string key)
            => Application.Current.TryFindResource(key)?.ToString() ?? key;
        #endregion

        #region================== UI ACTIONS ==================
        //----------------- BROWSE IMAGE -----------------
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    selectedImagePath = dlg.FileName;
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(selectedImagePath);
                    bmp.EndInit();
                    ImgPreview.Source = bmp;
                    PlaceholderText.Visibility = Visibility.Collapsed;

                    ToastManager.Success(GetStr("ImageLoaded"));
                }
                catch (Exception ex)
                {
                    ToastManager.Error($"{GetStr("ImageLoadFailed")}: {ex.Message}");
                }
            }
        }

        //----------------- SAVE LOCATION -----------------
        private void SaveLocation_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog { Filter = "Icon File|*.ico", FileName = "icon.ico" };
            if (dlg.ShowDialog() == true)
            {
                UpdateSavePathText(dlg.FileName);
            }
        }
        #endregion

        #region================== PATH EDITING ==================
        //----------------- CLICK HANDLERS -----------------
        private void PathContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (e.ClickCount == 2)
            {
                clickTimer.Stop();
                EnterPathEditMode();
                e.Handled = true;
            }
            else if (e.ClickCount == 1)
            {
                clickTimer.Start();
            }
        }

        private void SingleClickTimer_Tick(object sender, EventArgs e)
        {
            clickTimer.Stop();
            SaveLocation_Click(null, null);
        }

        //----------------- EDIT MODE -----------------
        private void EnterPathEditMode()
        {
            SavePathTxt.Visibility = Visibility.Collapsed;
            SavePathInput.Visibility = Visibility.Visible;

            SavePathInput.Text = SavePathTxt.Text == "..." ? "" : SavePathTxt.Text;
            SavePathInput.Focus();
            SavePathInput.SelectAll();
        }

        private void ExitPathEditMode()
        {
            SavePathInput.Visibility = Visibility.Collapsed;
            SavePathTxt.Visibility = Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(SavePathInput.Text))
            {
                UpdateSavePathText(SavePathInput.Text);
            }
        }

        //----------------- INPUT EVENTS -----------------
        private void SavePathInput_LostFocus(object sender, RoutedEventArgs e)
        {
            ExitPathEditMode();
        }

        private void SavePathInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExitPathEditMode();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SavePathInput.Visibility = Visibility.Collapsed;
                SavePathTxt.Visibility = Visibility.Visible;
                e.Handled = true;
            }
        }

        //----------------- UPDATE PATH -----------------
        private void UpdateSavePathText(string newPath)
        {
            SavePathTxt.Text = newPath;
            SavePathTxt.SetResourceReference(TextBlock.ForegroundProperty, "DynamicMainText");
        }
        #endregion

        #region================== CONVERSION LOGIC ==================
        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedImagePath) || string.IsNullOrEmpty(SavePathTxt.Text) || SavePathTxt.Text == "...")
            {
                ToastManager.Warning(GetStr("PleaseSelectPaths"));
                return;
            }

            try
            {
                BitmapImage sourceBmp = new BitmapImage(new Uri(selectedImagePath));
                int[] sizes = { 16, 32, 48, 64, 128, 256 };
                byte[][] iconImages = new byte[sizes.Length][];

                for (int i = 0; i < sizes.Length; i++)
                    iconImages[i] = GetPngBytes(sourceBmp, sizes[i]);

                using (FileStream fs = new FileStream(SavePathTxt.Text, FileMode.Create))
                {
                    fs.WriteByte(0); fs.WriteByte(0);
                    fs.WriteByte(1); fs.WriteByte(0);
                    fs.WriteByte((byte)sizes.Length); fs.WriteByte(0);

                    long iconDataOffset = 6 + (16 * sizes.Length);

                    for (int i = 0; i < sizes.Length; i++)
                    {
                        fs.WriteByte(sizes[i] == 256 ? (byte)0 : (byte)sizes[i]);
                        fs.WriteByte(sizes[i] == 256 ? (byte)0 : (byte)sizes[i]);
                        fs.WriteByte(0);
                        fs.WriteByte(0);
                        fs.WriteByte(1); fs.WriteByte(0);
                        fs.WriteByte(32); fs.WriteByte(0);

                        byte[] sizeBytes = BitConverter.GetBytes(iconImages[i].Length);
                        fs.Write(sizeBytes, 0, 4);

                        byte[] offsetBytes = BitConverter.GetBytes((int)iconDataOffset);
                        fs.Write(offsetBytes, 0, 4);

                        iconDataOffset += iconImages[i].Length;
                    }

                    for (int i = 0; i < sizes.Length; i++)
                        fs.Write(iconImages[i], 0, iconImages[i].Length);
                }

                MainWindow.icoPath = SavePathTxt.Text;

                ToastManager.Success(GetStr("IconSavedSuccess"));
                this.Close();
            }
            catch (Exception ex)
            {
                ToastManager.Error($"{GetStr("ConversionFailed")}: {ex.Message}");
            }
        }

        private byte[] GetPngBytes(BitmapSource source, int size)
        {
            var scale = new ScaleTransform((double)size / source.PixelWidth, (double)size / source.PixelHeight);
            var resized = new TransformedBitmap(source, scale);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(resized));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
        #endregion

        #region================== WINDOW CONTROLS ==================
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        #endregion
    }
}