using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Win32;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace rawinator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
                (Develop_Toggle_Gamma, nameof(RawImageProcessParams.UseAutoGamma)),
                (Develop_Toggle_Level, nameof(RawImageProcessParams.UseAutoLevel)),
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

            Develop_BorderColor_TextBox.LostFocus += Develop_BorderColor_TextBox_LostFocus;
            Develop_BorderWidth_TextBox.LostFocus += Develop_BorderWidth_TextBox_LostFocus;
            Develop_BorderWidth_TextBox.PreviewTextInput += Develop_BorderWidth_TextBox_PreviewTextInput;

            // Add event handlers for toggles
            Develop_Toggle_Enhance.Click += Develop_Toggle_Special_Click;
            Develop_Toggle_Denoise.Click += Develop_Toggle_Special_Click;
            Develop_Toggle_Gamma.Click += Develop_Toggle_Special_Click;
            Develop_Toggle_Level.Click += Develop_Toggle_Special_Click;
        }

        public SparseObservableList<RawImage> ImportedImages { get; set; } = [];

        private RawImage? _currentImage;
        public RawImage? CurrentImage {
            get => _currentImage;
            set {
                if (_currentImage != value)
                {
                    _currentImage = value;
                    OnPropertyChanged(nameof(CurrentImage));
                    OnPropertyChanged(nameof(HasImageSelected));
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        private void Library_Import_Button_Click(object sender, RoutedEventArgs e)
        {
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
                    MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete {selectedNumber} images from library? (files on disk won't be modified)", "Delete images", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                            perColor.Hue[color] = newValue;
                            UpdateDevelopImage();
                        }
                        break;
                    case "Saturation":
                        if (perColor.Saturation[color] != newValue)
                        {
                            perColor.Saturation[color] = newValue;
                            UpdateDevelopImage();
                        }
                        break;
                    case "Luminance":
                        if (perColor.Luminance[color] != newValue)
                        {
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
        }

        private void SetDevelopSliders()
        {
            if (CurrentImage == null)
            {
                ResetDevelopSliders();
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
            if (!int.TryParse(input, out int px) || px < 0)
            {
                tb.Text = ((int)CurrentImage.ProcessParams.BorderWidth).ToString();
                return;
            }
            if ((int)CurrentImage.ProcessParams.BorderWidth != px)
            {
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
            UpdateDevelopImage();
        }



        private void Menu_File_Open_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_File_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_File_Exit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Edit_Undo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Edit_Redo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Help_About_Click(object sender, RoutedEventArgs e)
        {

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
    }
}