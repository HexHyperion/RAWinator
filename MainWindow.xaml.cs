using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Win32;
using System.ComponentModel;
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
            DataContext = this;
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

        public bool HasImageSelected => CurrentImage != null;

        private RawImageProcessParams developImageParams = new();
        private bool isSliderDragged = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

            // Parallelism for faster import, but with a limit to avoid overwhelming the system
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

            var defines = new DngReadDefines {
                UseAutoWhitebalance = false,
                DisableAutoBrightness = false,
                UseCameraWhitebalance = true,
                InterpolationQuality = DngInterpolation.ModifiedAhd
            };
            var parallelOptions = new ParallelOptions {
                MaxDegreeOfParallelism = (int)Math.Round(Environment.ProcessorCount / 1.5)
            };
            bool errorOccurred = false;

            Parallel.For(0, images.Count, parallelOptions, (i, state) => {
                var img = images[i];
                try
                {
                    using var rawImage = new MagickImage();
                    rawImage.Settings.SetDefines(defines);
                    rawImage.Read(img.Path);
                    rawImage.AutoOrient();
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

        private void App_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App_TabControl.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Name == "Tabs_Develop")
                {
                    if (CurrentImage != null)
                    {
                        ResetSliders();
                        UpdateDevelopImage();
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
            if (sender is Slider slider && !isSliderDragged)
            {
                switch (slider.Name)
                {
                    case nameof(Develop_Slider_Exposure):
                        developImageParams.Exposure = slider.Value;
                        break;
                    case nameof(Develop_Slider_Brightness):
                        developImageParams.Brightness = slider.Value;
                        break;
                    case nameof(Develop_Slider_Highlights):
                        developImageParams.Highlights = slider.Value;
                        break;
                    case nameof(Develop_Slider_Shadows):
                        developImageParams.Shadows = slider.Value;
                        break;
                    case nameof(Develop_Slider_WhiteBalance):
                        developImageParams.WbTemperature = slider.Value;
                        break;
                    case nameof(Develop_Slider_WhiteBalanceTint):
                        developImageParams.WbTint = slider.Value;
                        break;
                    case nameof(Develop_Slider_Contrast):
                        developImageParams.Contrast = slider.Value;
                        break;
                    case nameof(Develop_Slider_Saturation):
                        developImageParams.Saturation = slider.Value;
                        break;
                    case nameof(Develop_Slider_Hue):
                        developImageParams.Hue = slider.Value;
                        break;
                }
                UpdateDevelopImage();
            }
        }

        private void UpdateDevelopImage()
        {
            if (CurrentImage == null) return;

            Dispatcher.Invoke(() => {
                Develop_Process_ProgressBar.IsIndeterminate = true;
                SetDevelopSlidersEnabled(false);
            });

            Task.Run(() => {
                MagickImage image = new();
                DngReadDefines defines = new() {
                    UseAutoWhitebalance = false,
                    DisableAutoBrightness = false,
                    UseCameraWhitebalance = true,
                    InterpolationQuality = DngInterpolation.ModifiedAhd
                };
                image.Settings.SetDefines(defines);
                image.Read(CurrentImage.Path);

                var adjusted = RawImageHelpers.ApplyAdjustments(
                    image,
                    developImageParams
                );

                Dispatcher.Invoke(() => {
                    Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(adjusted);
                    Develop_Process_ProgressBar.IsIndeterminate = false;
                    SetDevelopSlidersEnabled(true);
                });
            });
        }

        private void ResetSliders()
        {
            Develop_Slider_Exposure.Value = 0;
            Develop_Slider_Brightness.Value = 0;
            Develop_Slider_Highlights.Value = 0;
            Develop_Slider_Shadows.Value = 0;
            Develop_Slider_WhiteBalance.Value = 0;
            Develop_Slider_WhiteBalanceTint.Value = 0;
            Develop_Slider_Contrast.Value = 0;
            Develop_Slider_Saturation.Value = 0;
            Develop_Slider_Hue.Value = 0;
        }

        private void SetDevelopSlidersEnabled(bool enabled)
        {
            Develop_Slider_Exposure.IsEnabled = enabled;
            Develop_Slider_Brightness.IsEnabled = enabled;
            Develop_Slider_Highlights.IsEnabled = enabled;
            Develop_Slider_Shadows.IsEnabled = enabled;
            Develop_Slider_WhiteBalance.IsEnabled = enabled;
            Develop_Slider_WhiteBalanceTint.IsEnabled = enabled;
            Develop_Slider_Contrast.IsEnabled = enabled;
            Develop_Slider_Saturation.IsEnabled = enabled;
            Develop_Slider_Hue.IsEnabled = enabled;
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
    }
}