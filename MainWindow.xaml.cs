using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace rawinator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool isCropModeActive = false;
        private bool isDrawingCropRect = false;
        private Point cropStartPoint;
        private Point cropEndPoint;
        private Rectangle? cropOverlayRect = null;

        private Stack<RawImageProcessParams> undoStack = new();
        private Stack<RawImageProcessParams> redoStack = new();
        private const int MaxHistory = 30;

        public ObservableCollection<Preset> Presets { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            OpenCL.IsEnabled = true;
            DataContext = this;

            developSliders = [
                (Develop_Slider_Exposure, nameof(RawImageProcessParams.Exposure)),
                (Develop_Slider_Brightness, nameof(RawImageProcessParams.Brightness)),
                (Develop_Slider_Highlights, nameof(RawImageProcessParams.Highlights)),
                (Develop_Slider_Shadows, nameof(RawImageProcessParams.Shadows)),
                (Develop_Slider_WhiteBalance, nameof(RawImageProcessParams.WbTemperature)),
                (Develop_Slider_WhiteBalanceTint, nameof(RawImageProcessParams.WbTint)),
                (Develop_Slider_Contrast, nameof(RawImageProcessParams.Contrast)),
                (Develop_Slider_Saturation, nameof(RawImageProcessParams.Saturation)),
                (Develop_Slider_Hue, nameof(RawImageProcessParams.Hue)),
                (Develop_Slider_Sharpness, nameof(RawImageProcessParams.Sharpness)),
                (Develop_Slider_Noise, nameof(RawImageProcessParams.Noise)),
                (Develop_Slider_Vignette, nameof(RawImageProcessParams.Vignette)),
            ];
            developButtons = [
                (Develop_Toggle_Enhance, nameof(RawImageProcessParams.UseEnhance)),
                (Develop_Toggle_Denoise, nameof(RawImageProcessParams.UseDenoise)),
                (Develop_Toggle_AutoGamma, nameof(RawImageProcessParams.UseAutoGamma)),
                (Develop_Toggle_AutoLevel, nameof(RawImageProcessParams.UseAutoLevel)),
                (Develop_Toggle_Grayscale, nameof(RawImageProcessParams.UseGrayscale)),
                (Develop_Toggle_Sepia, nameof(RawImageProcessParams.UseSepia)),
                (Develop_Toggle_Solarize, nameof(RawImageProcessParams.UseSolarize)),
                (Develop_Toggle_Invert, nameof(RawImageProcessParams.UseInvert)),
                (Develop_Toggle_Charcoal, nameof(RawImageProcessParams.UseCharcoal)),
                (Develop_Toggle_OilPaint, nameof(RawImageProcessParams.UseOilPaint)),
                (Develop_Toggle_Sketch, nameof(RawImageProcessParams.UseSketch)),
                (Develop_Toggle_Posterize, nameof(RawImageProcessParams.UsePosterize)),
            ];

            colorSliders = [
                (Develop_Slider_Red, HslColorRange.Red),
                (Develop_Slider_Orange, HslColorRange.Orange),
                (Develop_Slider_Yellow, HslColorRange.Yellow),
                (Develop_Slider_Green, HslColorRange.Green),
                (Develop_Slider_Aqua, HslColorRange.Aqua),
                (Develop_Slider_Blue, HslColorRange.Blue),
                (Develop_Slider_Purple, HslColorRange.Purple),
                (Develop_Slider_Magenta, HslColorRange.Magenta),
            ];

            Develop_Image.RenderTransform = new TransformGroup
            {
                Children = [
                    new ScaleTransform(1, 1),
                    new TranslateTransform(0, 0)
                ]
            };

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            LoadPresets();
        }

        public SparseObservableList<RawImage> ImportedImages { get; set; } = new();

        private RawImage? _currentImage;
        public RawImage? CurrentImage {
            get => _currentImage;
            set {
                if (_currentImage != value)
                {
                    _currentImage = value;
                    undoStack.Clear();
                    redoStack.Clear();
                    OnPropertyChanged(nameof(CurrentImage));
                    OnPropertyChanged(nameof(HasImageSelected));
                    SetAllDevelopSliders();
                }
            }
        }
        public MagickImage? CurrentDevelopImage;

        public bool HasImageSelected => CurrentImage != null;

        private bool isSliderDragged = false;
        private (Slider slider, string property)[] developSliders;
        private string currentColorAdjustmentType = "Hue";
        private (Slider slider, HslColorRange color)[] colorSliders;
        private (ToggleButton button, string property)[] developButtons;

        private double imageZoom = 1.0;
        private const double ZoomStep = 1.15;
        private const double MinZoom = 0.1;
        private const double MaxZoom = 10.0;
        private Point imageOffset = new(0, 0);
        private Point? dragStart = null;
        private Point dragOrigin = new(0, 0);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        private void Library_Import_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmImportOverwrite())
                return;

            OpenFileDialog openFileDialog = new() {
                Filter = "RAW files|*.arw;*.cr2;*.cr3;*.nef;*.nrw;*.orf;*.pef;*.raf;*.rw2;*.srw;*.dng;*.k25;*.kdc;*.srf;*.sr2;*.mos;*.3fr;*.fff;*.rwl;*.iiq",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true && openFileDialog.FileNames != null)
            {
                Task.Run(() => {
                    ImportImages(openFileDialog.FileNames);
                });
            }
        }

        private void Library_Export_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog saveToFolderDialog = new() {
                Title = "Select Export Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (Library_Image_Grid.SelectedItems.Count > 0 && saveToFolderDialog.ShowDialog() == true)
            {
                var selectedImages = Library_Image_Grid.SelectedItems.Cast<RawImage>().ToList();
                Task.Run(() => {
                    ExportImages(selectedImages, saveToFolderDialog.FolderName);
                });
            }
        }

        private void Library_Image_Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Library_Image_Grid.SelectedItems != null)
            {
                int selectedNumber = Library_Image_Grid.SelectedItems.Count;
                if (selectedNumber > 1)
                {
                    MessageBoxResult result = MessageBox.Show($"Delete {selectedNumber} images from library? Files on disk won't be modified.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        var itemsToDelete = Library_Image_Grid.SelectedItems.Cast<RawImage>().ToList();
                        foreach (var img in itemsToDelete)
                        {
                            ImportedImages.Remove(img);
                        }
                    }
                }
                else if (Library_Image_Grid.SelectedItem is RawImage img)
                {
                    ImportedImages.Remove(img);
                }
            }
        }

        private void Library_Image_Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Library_Image_Grid.SelectedItem is RawImage selectedImage)
            {
                CurrentImage = selectedImage;
                Library_Image_Thumbnail.Source = selectedImage.SmallThumbnail;

                WindowHelpers.DisplayMetadata(Library_Image_Metadata_Panel, selectedImage, true);
            }
            else
            {
                CurrentImage = null;
                Library_Image_Thumbnail.Source = null;
                Library_Image_Metadata_Panel.Children.Clear();
                Library_Export_Button.IsEnabled = false;
            }
            if (Library_Image_Grid.SelectedItems.Count > 0)
            {
                Library_Export_Button.IsEnabled = true;
            }
        }



        private void Tabs_View_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (View_Image_List.SelectedIndex > 0)
                {
                    View_Image_List.SelectedIndex--;
                }
            }
            else if (e.Key == Key.Right)
            {
                if (View_Image_List.SelectedIndex < View_Image_List.Items.Count - 1)
                {
                    View_Image_List.SelectedIndex++;
                }
            }
        }

        private void View_Image_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (View_Image_List.SelectedItem is RawImage selected)
            {
                CurrentImage = selected;
                Task.Run(() => {
                    var thumbnail = CurrentImage.GetFullThumbnail();
                    Dispatcher.Invoke(() => {
                        View_Image.Source = thumbnail;
                    });
                });

                WindowHelpers.DisplayMetadata(View_Metadata_StackPanel, selected, true);
                WindowHelpers.DisplayMetadata(View_AllMetadata_StackPanel, selected, false);
            }
        }

        private void View_Image_List_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                if (VisualTreeHelper.GetChild(listBox, 0) is Border border &&
                    VisualTreeHelper.GetChild(border, 0) is ScrollViewer scrollViewer)
                {
                    double scrollAmount = Math.Sign(e.Delta) * 10;
                    double offset = scrollViewer.HorizontalOffset - scrollAmount;
                    scrollViewer.ScrollToHorizontalOffset(offset);
                    e.Handled = true;
                }
            }
        }



        private void Develop_Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            isSliderDragged = true;
        }

        private void Develop_Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isSliderDragged = false;
            Develop_Slider_ValueChanged(sender, new RoutedPropertyChangedEventArgs<double>(0, 0));
        }

        private void Develop_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && !isSliderDragged && CurrentImage != null)
            {
                var match = developSliders.FirstOrDefault(ds => ds.slider == slider);
                if (!string.IsNullOrEmpty(match.property))
                {
                    var prop = typeof(RawImageProcessParams).GetProperty(match.property);
                    if (prop != null)
                    {
                        double currentValue = (double)prop.GetValue(CurrentImage.ProcessParams)!;
                        double newValue = slider.Value;
                        if (!currentValue.Equals(newValue))
                        {
                            UpdateUndoHistory();
                            prop.SetValue(CurrentImage.ProcessParams, newValue);
                            UpdateDevelopImage();
                        }
                    }
                }
            }
        }


        private void ColorAdjustmentTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Content is string type)
            {
                currentColorAdjustmentType = type;
                SetColorSliders();
            }
        }

        private void Develop_ColorSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isSliderDragged = false;
            Develop_ColorSlider_ValueChanged(sender, new RoutedPropertyChangedEventArgs<double>(0, 0));
        }

        private void Develop_ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider &&
                slider.Tag is string tag &&
                !isSliderDragged && CurrentImage != null &&
                Enum.TryParse<HslColorRange>(tag, out var color))
            {
                var perColor = CurrentImage.ProcessParams.PerColor;
                double newValue = slider.Value;
                switch (currentColorAdjustmentType)
                {
                    case "Hue":
                        if (perColor.Hue[color] != newValue)
                        {
                            UpdateUndoHistory();
                            perColor.Hue[color] = newValue;
                            UpdateDevelopImage();
                        }
                        break;
                    case "Saturation":
                        if (perColor.Saturation[color] != newValue)
                        {
                            UpdateUndoHistory();
                            perColor.Saturation[color] = newValue;
                            UpdateDevelopImage();
                        }
                        break;
                    case "Luminance":
                        if (perColor.Luminance[color] != newValue)
                        {
                            UpdateUndoHistory();
                            perColor.Luminance[color] = newValue;
                            UpdateDevelopImage();
                        }
                        break;
                }
            }
        }


        private void ResetDevelopSliders()
        {
            foreach (var (slider, _) in developSliders)
                slider.Value = 0;
            foreach (var (slider, _) in colorSliders)
                slider.Value = 0;
            foreach (var (button, _) in developButtons)
                button.IsChecked = false;
        }

        private void SetDevelopSlidersEnabled(bool enabled)
        {
            foreach (var (slider, _) in developSliders)
                slider.IsEnabled = enabled;
            foreach (var (slider, _) in colorSliders)
                slider.IsEnabled = enabled;
            foreach (var (button, _) in developButtons)
                button.IsEnabled = enabled;
            Develop_BorderColor_TextBox.IsEnabled = enabled;
            Develop_BorderWidth_TextBox.IsEnabled = enabled;
        }

        private void SetDevelopSliders()
        {
            if (CurrentImage == null)
            {
                ResetDevelopSliders();
                Develop_BorderColor_TextBox.Text = "#ffffff";
                Develop_BorderWidth_TextBox.Text = "0";
                return;
            }
            var processParams = CurrentImage.ProcessParams;
            foreach (var (slider, property) in developSliders)
            {
                var prop = typeof(RawImageProcessParams).GetProperty(property);
                if (prop != null)
                    slider.Value = (double)prop.GetValue(processParams)!;
            }
            foreach (var (button, property) in developButtons)
            {
                var prop = typeof(RawImageProcessParams).GetProperty(property);
                if (prop != null)
                    button.IsChecked = (bool)prop.GetValue(processParams)!;
            }
            foreach (var (slider, color) in colorSliders)
            {
                foreach (var prop in typeof(RawImageProcessParams.ColorAdjustments).GetProperties())
                {
                    if (prop.Name == currentColorAdjustmentType)
                    {
                        var perColor = processParams.PerColor;
                        if (perColor != null && perColor.GetType().GetProperty(color.ToString()) is PropertyInfo colorProp)
                        {
                            slider.Value = (double)colorProp.GetValue(perColor)!;
                        }
                    }
                }
            }
            Develop_BorderColor_TextBox.Text = processParams.BorderColor ?? "#ffffff";
            Develop_BorderWidth_TextBox.Text = processParams.BorderWidth.ToString();
        }

        private void SetColorSliders()
        {
            if (CurrentImage == null) return;
            var perColor = CurrentImage.ProcessParams.PerColor;
            foreach (var (slider, color) in colorSliders)
            {
                double value = 0;
                switch (currentColorAdjustmentType)
                {
                    case "Hue":
                    {
                        value = perColor.Hue[color];
                        break;
                    }
                    case "Saturation":
                    {
                        value = perColor.Saturation[color];
                        break;
                    }
                    case "Luminance":
                    {
                        value = perColor.Luminance[color];
                        break;
                    }
                }
                slider.Value = value;
            }
        }

        private void SetAllDevelopSliders()
        {
            SetDevelopSliders();
            SetColorSliders();
        }

        private void UpdateDevelopImage(bool isNew = false)
        {
            if (CurrentImage == null)
            {
                Develop_Image.Source = null;
                return;
            }
            Develop_Process_ProgressBar.IsIndeterminate = true;
            SetDevelopSlidersEnabled(false);

            Task.Run(() => {
                if (isNew || CurrentDevelopImage == null)
                {
                    CurrentDevelopImage = CurrentImage.GetRawImage();
                }
                var adjusted = RawImageHelpers.ApplyAdjustments(CurrentDevelopImage, CurrentImage.ProcessParams);

                Dispatcher.Invoke(() => {
                    Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(adjusted);
                    Develop_Process_ProgressBar.IsIndeterminate = false;
                    if (isNew == true)
                    {
                        SetAllDevelopSliders();
                        SetImageZoom(1.0, true);
                    }
                    SetDevelopSlidersEnabled(true);
                });
            });
        }

        private void Develop_BorderColor_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CurrentImage == null) return;
            if (sender is not TextBox tb) return;
            string input = tb.Text.Trim();
            // Validate hex color (allow #RGB, #RRGGBB, #AARRGGBB)
            if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^#([0-9a-fA-F]{3,8})$"))
            {
                if (CurrentImage.ProcessParams.BorderColor != input)
                {
                    UpdateUndoHistory();
                    CurrentImage.ProcessParams.BorderColor = input;
                    UpdateDevelopImage();
                }
            }
            else
            {
                tb.Text = CurrentImage.ProcessParams.BorderColor ?? "#ffffff";
                return;
            }
        }

        private void Develop_BorderWidth_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CurrentImage == null) return;
            if (sender is not TextBox tb) return;
            string input = tb.Text.Trim();
            if (!uint.TryParse(input, out uint px) || px < 0)
            {
                tb.Text = (CurrentImage.ProcessParams.BorderWidth).ToString();
                return;
            }
            if (CurrentImage.ProcessParams.BorderWidth != px)
            {
                UpdateUndoHistory();
                CurrentImage.ProcessParams.BorderWidth = px;
                UpdateDevelopImage();
            }
        }

        private void Develop_BorderWidth_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void Develop_Toggle_Special_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentImage == null) return;
            if (sender is not ToggleButton toggle) return;
            string? toggleName = toggle.Name;
            if (string.IsNullOrEmpty(toggleName) || !toggleName.StartsWith("Develop_Toggle_")) return;
            string propertyName = "Use" + toggleName["Develop_Toggle_".Length..];

            var prop = typeof(RawImageProcessParams).GetProperty(propertyName);
            if (prop != null)
            {
                UpdateUndoHistory();
                prop.SetValue(CurrentImage.ProcessParams, toggle.IsChecked == true);
                UpdateDevelopImage();
            }
        }


        private void ZoomIn_Button_Click(object sender, RoutedEventArgs e)
        {
            SetImageZoom(imageZoom * ZoomStep);
        }

        private void ZoomOut_Button_Click(object sender, RoutedEventArgs e)
        {
            SetImageZoom(imageZoom / ZoomStep);
        }

        private void ZoomReset_Button_Click(object sender, RoutedEventArgs e)
        {
            SetImageZoom(1.0, true);
        }

        private void SetImageZoom(double zoom, bool recenter = false)
        {
            imageZoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            if (recenter)
            {
                imageOffset = new Point(0, 0);
            }
            ApplyImageTransform();
        }

        private void ApplyImageTransform()
        {
            if (Develop_Image.RenderTransform is TransformGroup tg &&
                tg.Children[0] is ScaleTransform st &&
                tg.Children[1] is TranslateTransform tt)
            {
                st.ScaleX = st.ScaleY = imageZoom;
                tt.X = imageOffset.X;
                tt.Y = imageOffset.Y;
            }
        }

        private void Develop_Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (imageZoom != 1.0)
            {
                dragStart = e.GetPosition(Develop_Image_Container);
                dragOrigin = imageOffset;
                Develop_Image.CaptureMouse();
            }
        }

        private void Develop_Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStart = null;
            Develop_Image.ReleaseMouseCapture();
        }

        private void Develop_Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragStart.HasValue && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(Develop_Image_Container);
                var delta = pos - dragStart.Value;
                imageOffset = new Point(dragOrigin.X + delta.X, dragOrigin.Y + delta.Y);
                ApplyImageTransform();
            }
        }

        private void Develop_Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? ZoomStep : (1.0 / ZoomStep);
            SetImageZoom(imageZoom * zoomFactor);
            e.Handled = true;
        }



        private void Menu_File_Open_Click(object sender, RoutedEventArgs e)
        {
            ImportAndGoToLibrary();
        }

        private void Menu_File_Save_Click(object sender, RoutedEventArgs e)
        {
            ExportSelectedAndGoToLibrary();
        }

        private void Menu_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitWithConfirmation();
        }

        private void Menu_Edit_Undo_Click(object sender, RoutedEventArgs e)
        {
            UndoDevelopEdit();
        }

        private void Menu_Edit_Redo_Click(object sender, RoutedEventArgs e)
        {
            RedoDevelopEdit();
        }

        private void Menu_Help_About_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow {
                Owner = this
            };
            about.ShowDialog();
        }



        private void App_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App_TabControl.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Name == "Tabs_Develop")
                {
                    if (CurrentImage != null)
                    {
                        UpdateDevelopImage(true);
                    }
                }
                else if (selectedTab.Name == "Tabs_View")
                {
                    if (CurrentImage != null)
                    {
                        Task.Run(() => {
                            var thumbnail = CurrentImage.GetFullThumbnail();
                            Dispatcher.Invoke(() => {
                                View_Image.Source = thumbnail;
                            });
                        });
                        OnPropertyChanged(nameof(CurrentImage));
                    }
                }
            }
        }



        private void ImportImages(string[] filenames)
        {
            Dispatcher.Invoke(() => {
                ImportedImages.Clear();
                Library_Import_ProgressBar.Maximum = filenames.Length;
                Library_Import_ProgressBar.Value = 0;
                Library_Import_Status.Visibility = Visibility.Visible;
                Library_Import_Button.Content = "Importing...";
                Library_Import_Button.IsEnabled = false;
            });

            var parallelOptions = new ParallelOptions {
                MaxDegreeOfParallelism = (int)Math.Round(Environment.ProcessorCount / 1.5)
            };
            Parallel.For(0, filenames.Length, parallelOptions, i => {
                var image = new RawImage(filenames[i]);
                Dispatcher.Invoke(() => {
                    ImportedImages[i] = image;
                    Library_Import_ProgressBar.Value++;
                });
            });

            Dispatcher.Invoke(() => {
                Library_Image_Grid.SelectedIndex = 0;
                Library_Import_Status.Visibility = Visibility.Collapsed;
                Library_Import_Button.Content = "Import...";
                Library_Import_Button.IsEnabled = true;
                Library_Import_ProgressBar.Value = 0;
                Library_Import_ProgressBar.Maximum = 1;
            });
        }


        private void ExportImages(List<RawImage> images, string exportPath)
        {
            Dispatcher.Invoke(() => {
                Library_Import_ProgressBar.Maximum = images.Count;
                Library_Import_ProgressBar.Value = 0;
                Library_Import_Status.Visibility = Visibility.Visible;
                Library_Export_Button.Content = "Exporting...";
                Library_Export_Button.IsEnabled = false;
                Library_Import_Button.IsEnabled = false;
            });

            var parallelOptions = new ParallelOptions {
                MaxDegreeOfParallelism = (int)Math.Round(Environment.ProcessorCount / 1.5)
            };
            bool errorOccurred = false;

            Parallel.For(0, images.Count, parallelOptions, (i, state) => {
                var img = images[i];
                try
                {
                    using var rawImage = RawImageHelpers.ApplyAdjustments(img.GetRawImage(), img.ProcessParams);
                    rawImage.ColorSpace = ColorSpace.sRGB;
                    rawImage.Format = MagickFormat.Jpeg;
                    string outputFileName = System.IO.Path.GetFileNameWithoutExtension(img.Filename) + ".jpg";
                    string outputPath = System.IO.Path.Combine(exportPath, outputFileName);
                    rawImage.Write(outputPath);
                }
                catch (Exception ex)
                {
                    if (!errorOccurred)
                    {
                        errorOccurred = true;
                        Dispatcher.Invoke(() => {
                            MessageBox.Show($"Failed to export image {img.Filename}: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    state.Stop();
                }
                finally
                {
                    Dispatcher.Invoke(() => {
                        Library_Import_ProgressBar.Value++;
                    });
                }
            });

            Dispatcher.Invoke(() => {
                Library_Import_Status.Visibility = Visibility.Collapsed;
                Library_Export_Button.Content = "Export...";
                Library_Export_Button.IsEnabled = true;
                Library_Import_Button.IsEnabled = true;
                Library_Import_ProgressBar.Value = 0;
                Library_Import_ProgressBar.Maximum = 1;
            });
        }


        private void Develop_Toggle_Crop_Checked(object sender, RoutedEventArgs e)
        {
            if (CurrentImage == null || CurrentDevelopImage == null)
            {
                Develop_Toggle_Crop.IsChecked = false;
                return;
            }
            ToggleCropUI(true);
            EnableCropMode(true);
            SetImageZoom(1.0, true);
            Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(CurrentDevelopImage);
        }
        private void Develop_Toggle_Crop_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateUndoHistory();
            ToggleCropUI(false);
            EnableCropMode(false);
            UpdateDevelopImage();
        }

        private void ToggleCropUI(bool cropMode)
        {
            Develop_Sliders_Panel_Quick.Visibility = cropMode ? Visibility.Collapsed : Visibility.Visible;
            Develop_Sliders_Panel_Crop.Visibility = cropMode ? Visibility.Visible : Visibility.Collapsed;
            SetDevelopSlidersEnabled(!cropMode);
        }

        private void EnableCropMode(bool enable)
        {
            isCropModeActive = enable;
            Develop_Image_OverlayCanvas.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            Develop_ZoomPanel.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            if (enable)
            {
                Develop_Image_OverlayCanvas.MouseLeftButtonDown += OverlayCanvas_MouseLeftButtonDown;
                Develop_Image_OverlayCanvas.MouseMove += OverlayCanvas_MouseMove;
                Develop_Image_OverlayCanvas.MouseLeftButtonUp += OverlayCanvas_MouseLeftButtonUp;
            }
            else
            {
                Develop_Image_OverlayCanvas.Children.Clear();
                Develop_Image_OverlayCanvas.MouseLeftButtonDown -= OverlayCanvas_MouseLeftButtonDown;
                Develop_Image_OverlayCanvas.MouseMove -= OverlayCanvas_MouseMove;
                Develop_Image_OverlayCanvas.MouseLeftButtonUp -= OverlayCanvas_MouseLeftButtonUp;
                isDrawingCropRect = false;
                cropOverlayRect = null;
            }
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isCropModeActive) return;

            cropStartPoint = GetClampedImagePoint(e.GetPosition(Develop_Image_OverlayCanvas));
            cropEndPoint = cropStartPoint;
            isDrawingCropRect = true;

            if (cropOverlayRect == null)
            {
                cropOverlayRect = new Rectangle {
                    Stroke = Brushes.Yellow,
                    StrokeThickness = 1,
                    StrokeDashArray = [2, 2],
                    IsHitTestVisible = false
                };
                Develop_Image_OverlayCanvas.Children.Add(cropOverlayRect);
            }

            Canvas.SetLeft(cropOverlayRect, cropStartPoint.X);
            Canvas.SetTop(cropOverlayRect, cropStartPoint.Y);
            cropOverlayRect.Width = 0;
            cropOverlayRect.Height = 0;
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isCropModeActive || !isDrawingCropRect || cropOverlayRect == null) return;

            cropEndPoint = GetClampedImagePoint(e.GetPosition(Develop_Image_OverlayCanvas));

            var (imgW, imgH, _, _, scale, offsetX, offsetY) = GetImageMetrics();

            double x0 = cropStartPoint.X, y0 = cropStartPoint.Y;
            double x1 = cropEndPoint.X, y1 = cropEndPoint.Y;
            double dx = x1 - x0, dy = y1 - y0;

            double rectX = x0, rectY = y0, rectW = dx, rectH = dy;

            if (TryGetAspectRatio(out double aspect))
            {
                WindowHelpers.AdjustRectForAspect(ref rectW, ref rectH, aspect);
                WindowHelpers.ClampRectToCanvas(ref rectX, ref rectY, ref rectW, ref rectH,
                                                offsetX, offsetY, imgW * scale, imgH * scale);
            }

            WindowHelpers.NormalizeRect(ref rectX, ref rectW);
            WindowHelpers.NormalizeRect(ref rectY, ref rectH);
            WindowHelpers.ClampRectToBounds(ref rectX, ref rectY, ref rectW, ref rectH,
                                            offsetX, offsetY, imgW * scale, imgH * scale);

            Canvas.SetLeft(cropOverlayRect, rectX);
            Canvas.SetTop(cropOverlayRect, rectY);
            cropOverlayRect.Width = rectW;
            cropOverlayRect.Height = rectH;
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isCropModeActive || !isDrawingCropRect || cropOverlayRect == null) return;
            isDrawingCropRect = false;

            if (CurrentImage != null)
            {
                var (imgW, imgH, _, _, scale, offsetX, offsetY) = GetImageMetrics();

                double left = Canvas.GetLeft(cropOverlayRect);
                double top = Canvas.GetTop(cropOverlayRect);
                double width = cropOverlayRect.Width;
                double height = cropOverlayRect.Height;

                double imgX = (left - offsetX) / scale;
                double imgY = (top - offsetY) / scale;
                double imgWRect = width / scale;
                double imgHRect = height / scale;

                double normX = Math.Clamp(imgX / imgW, 0, 1);
                double normY = Math.Clamp(imgY / imgH, 0, 1);
                double normW = Math.Clamp(imgWRect / imgW, 0, 1 - normX);
                double normH = Math.Clamp(imgHRect / imgH, 0, 1 - normY);

                CurrentImage.ProcessParams.CropX = normX;
                CurrentImage.ProcessParams.CropY = normY;
                CurrentImage.ProcessParams.CropWidth = normW;
                CurrentImage.ProcessParams.CropHeight = normH;
            }
        }

        private Point GetClampedImagePoint(Point canvasPoint)
        {
            var (imgW, imgH, _, _, scale, offsetX, offsetY) = GetImageMetrics();
            double dispW = imgW * scale;
            double dispH = imgH * scale;

            double x = Math.Clamp(canvasPoint.X, offsetX, offsetX + dispW);
            double y = Math.Clamp(canvasPoint.Y, offsetY, offsetY + dispH);
            return new Point(x, y);
        }

        private bool TryGetAspectRatio(out double aspect)
        {
            aspect = 0;
            double h = 0;
            bool valid = double.TryParse(Develop_TextBox_AspectWidth.Text, out double w) && w > 0 &&
                         double.TryParse(Develop_TextBox_AspectHeight.Text, out h) && h > 0;
            if (valid) aspect = w / h;
            return valid;
        }

        private (double imgW, double imgH, double canvasW, double canvasH, double scale, double offsetX, double offsetY) GetImageMetrics()
        {
            if (Develop_Image.Source is not BitmapSource bmp)
                return (0, 0, 0, 0, 1, 0, 0);

            double imgW = bmp.PixelWidth;
            double imgH = bmp.PixelHeight;
            double canvasW = Develop_Image_OverlayCanvas.ActualWidth;
            double canvasH = Develop_Image_OverlayCanvas.ActualHeight;
            double scale = Math.Min(canvasW / imgW, canvasH / imgH);
            double dispW = imgW * scale;
            double dispH = imgH * scale;
            double offsetX = (canvasW - dispW) / 2;
            double offsetY = (canvasH - dispH) / 2;

            return (imgW, imgH, canvasW, canvasH, scale, offsetX, offsetY);
        }

        // These are usually used for one-time corrections and are not destructive,
        // so we can apply them directly to the current image.
        private void Develop_Button_Rotate_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDevelopImage == null) return;
            CurrentDevelopImage.Rotate(-90);
            Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(CurrentDevelopImage);
        }
        private void Develop_Button_FlipX_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDevelopImage == null) return;
            CurrentDevelopImage.Flip();
            Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(CurrentDevelopImage);
        }
        private void Develop_Button_FlipY_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDevelopImage == null) return;
            CurrentDevelopImage.Flop();
            Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(CurrentDevelopImage);
        }


        private void UpdateUndoHistory()
        {
            if (CurrentImage == null) return;
            undoStack.Push(CurrentImage.ProcessParams.Clone());
            while (undoStack.Count > MaxHistory)
                undoStack = new Stack<RawImageProcessParams>(undoStack.Reverse().Take(MaxHistory).Reverse());
            redoStack.Clear();
        }

        private void UndoDevelopEdit()
        {
            if (undoStack.Count == 0 || CurrentImage == null) return;
            redoStack.Push(CurrentImage.ProcessParams.Clone());
            var prev = undoStack.Pop();
            CurrentImage.ProcessParams.CopyFrom(prev);
            SetAllDevelopSliders();
            UpdateDevelopImage();
        }

        private void RedoDevelopEdit()
        {
            if (redoStack.Count == 0 || CurrentImage == null) return;
            undoStack.Push(CurrentImage.ProcessParams.Clone());
            var next = redoStack.Pop();
            CurrentImage.ProcessParams.CopyFrom(next);
            SetAllDevelopSliders();
            UpdateDevelopImage();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBoxBase)
                return;

            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    RedoDevelopEdit();
                    e.Handled = true;
                }
                else
                {
                    UndoDevelopEdit();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                RedoDevelopEdit();
                e.Handled = true;
            }
            if (e.Key == Key.E && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ExportSelectedAndGoToLibrary();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ImportAndGoToLibrary();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ExitWithConfirmation();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.I && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var about = new AboutWindow { Owner = this };
                about.ShowDialog();
                e.Handled = true;
                return;
            }
        }

        private void SwitchToLibraryTab()
        {
            if (App_TabControl != null && Tabs_Library != null)
                App_TabControl.SelectedItem = Tabs_Library;
        }

        private bool ConfirmImportOverwrite()
        {
            if (ImportedImages.Count > 0)
            {
                var result = MessageBox.Show(
                    "Importing new images will remove all currently imported images and their edits. Continue?",
                    "Confirm Import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                return result == MessageBoxResult.Yes;
            }
            return true;
        }

        private void ImportAndGoToLibrary()
        {
            SwitchToLibraryTab();
            Library_Import_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void ExportSelectedAndGoToLibrary()
        {
            SwitchToLibraryTab();
            Library_Export_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private bool ConfirmExit()
        {
            if (ImportedImages.Count > 0)
            {
                var result = MessageBox.Show(
                    "Exiting now will lose all imported images and their edits. Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                return result == MessageBoxResult.Yes;
            }
            return true;
        }

        private void ExitWithConfirmation()
        {
            if (!ConfirmExit())
                return;
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!ConfirmExit())
            {
                e.Cancel = true;
            }
        }


        private void Add_Preset_Button_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentImage == null) return;
            var dialog = new InputDialog("Enter preset name:") {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {
                string? name = dialog.ResponseText?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    var existing = Presets.FirstOrDefault(p => p.Name == name);
                    if (existing != null)
                    {
                        Presets.Remove(existing);
                    }

                    Presets.Add(new Preset(name, CurrentImage.ProcessParams));
                    SavePresets();
                }
            }
        }

        private void Preset_ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Preset_ListBox.SelectedItem is Preset preset && CurrentImage != null)
            {
                UpdateUndoHistory();
                CurrentImage.ProcessParams.CopyFrom(preset.Params);
                SetAllDevelopSliders();
                UpdateDevelopImage();
            }
        }

        private void Preset_ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Preset_ListBox.SelectedItems.Count > 0)
            {
                foreach (var item in Preset_ListBox.SelectedItems.OfType<Preset>().ToList())
                {
                    Presets.Remove(item);
                }
                SavePresets();
            }
        }

        private void SavePresets()
        {
            try
            {
                var list = Presets.Select(p => new PresetDTO { Name = p.Name, Params = p.Params }).ToList();
                File.WriteAllText("presets.json", JsonSerializer.Serialize(list));
            }
            catch {}
        }

        private void LoadPresets()
        {
            try
            {
                if (File.Exists("presets.json"))
                {
                    var list = JsonSerializer.Deserialize<List<PresetDTO>>(File.ReadAllText("presets.json"));
                    if (list != null)
                    {
                        Presets.Clear();
                        foreach (var dto in list)
                        {
                            Presets.Add(new Preset(dto.Name, dto.Params));
                        }
                    }
                }
            }
            catch {}
        }

        private class PresetDTO
        {
            public required string Name { get; set; }
            public required RawImageProcessParams Params { get; set; }
        }
    }
}