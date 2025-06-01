using ImageMagick;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace rawinator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public SparseObservableList<RawImage> ImportedImages { get; set; } = [];
        public RawImage? CurrentImage = null;
        private RawImageProcessParams developImageParams = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
        private Thread imageImportThread;
        private Thread imageProcessThread;
        private bool isSliderDragged = false;

        private void ImportImages(string[] filenames)
        {
            Dispatcher.Invoke(() => {
                ImportedImages.Clear();
                Library_Import_ProgressBar.Maximum = filenames.Length;
                Library_Import_Status.Visibility = Visibility.Visible;
                Library_Import_Button.Content = $"Importing...";
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

        private void Library_Import_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new() {
                Filter = "RAW files|*.arw;*.cr2;*.cr3;*.nef;*.nrw;*.orf;*.pef;*.raf;*.rw2;*.srw;*.dng;*.k25;*.kdc;*.srf;*.sr2;*.mos;*.3fr;*.fff;*.rwl;*.iiq",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true && openFileDialog.FileNames != null)
            {
                imageImportThread = new Thread(() => {
                    ImportImages(openFileDialog.FileNames);
                });
                imageImportThread.Start();
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
                Library_Image_Thumbnail.Source = selectedImage.SmallThumbnail;

                var imageDimensions = selectedImage.GetMetadata(MetadataTagLists.ImageDimensions);

                var metadataString = new StringBuilder();
                metadataString.AppendLine($"Filename: {selectedImage.Filename}");
                metadataString.AppendLine($"Image Size: {imageDimensions[0].Item2} x {imageDimensions[1].Item2} pixels\n");

                foreach (var tag in selectedImage.GetMetadata(MetadataTagLists.General))
                {
                    if (string.IsNullOrEmpty(tag.Item1))
                    {
                        metadataString.AppendLine("");
                        continue;
                    }
                    metadataString.AppendLine($"{tag.Item1}: {tag.Item2}");
                }
                Library_Image_Metadata_Text.Text = metadataString.ToString();

                CurrentImage = selectedImage;
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
                    else
                    {
                        MessageBox.Show("Please select an image from the library to develop.", "No Image Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        App_TabControl.SelectedItem = Tabs_Library;
                    }
                }
                else if (selectedTab.Name == "Tabs_View")
                {
                    if (CurrentImage != null)
                    {
                        View_Image.Source = CurrentImage.GetFullThumbnail();
                    }
                }
            }
        }

        private void View_Image_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (View_Image_List.SelectedItem is RawImage selected)
            {
                CurrentImage = selected;
                View_Image.Source = CurrentImage.GetFullThumbnail();
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
                Develop_Process_Text.Visibility = Visibility.Visible;
                SetDevelopSlidersEnabled(false);
            });

            imageProcessThread = new Thread(() =>
            {
                var adjusted = RawImageHelpers.ApplyAdjustments(
                    new MagickImage(CurrentImage.Path),
                    developImageParams
                );

                Dispatcher.Invoke(() =>
                {
                    Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(adjusted);
                    Develop_Process_ProgressBar.IsIndeterminate = false;
                    Develop_Process_Text.Visibility = Visibility.Collapsed;
                    SetDevelopSlidersEnabled(true);
                });
            });
            imageProcessThread.Start();
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
    }
}