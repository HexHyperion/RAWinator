using ImageMagick;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace rawinator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public ObservableCollection<RawImage> importedImages { get; set; } = [];
        private RawImage? developImage = null;
        private RawImageProcessParams developImageParams = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
        private Thread imageImportThread;

        private void ImportImages(string[] filenames)
        {
            Dispatcher.Invoke(() => {
                Library_Import_Button.Content = "Importing...";
                Library_Import_Button.IsEnabled = false;
            });
            // Parallelism for faster import
            Parallel.ForEach(filenames, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, filename => {
                var image = new RawImage(filename);
                Dispatcher.Invoke(() => {
                    importedImages.Add(image);
                });
            });

            Dispatcher.Invoke(() => {
                Library_Image_Grid.SelectedIndex = 0;
                Library_Import_Button.Content = "Import...";
                Library_Import_Button.IsEnabled = true;
            });
        }

        private void Library_Import_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "RAW files|*.arw;*.cr2;*.cr3;*.nef;*.nrw;*.orf;*.pef;*.raf;*.rw2;*.srw;*.dng;*.k25;*.kdc;*.srf;*.sr2;*.mos;*.3fr;*.fff;*.rwl;*.iiq";
            openFileDialog.Multiselect = true;
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
                            importedImages.Remove(img);
                        }
                    }
                }
                else if (Library_Image_Grid.SelectedItem is RawImage img)
                {
                    importedImages.Remove(img);
                }
            }
        }

        private void Library_Image_Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Library_Image_Grid.SelectedItem is RawImage selectedImage)
            {
                Library_Image_Thumbnail.Source = selectedImage.SmallThumbnail;
                Library_Image_Metadata.Content = selectedImage.GetMetadataString();

                // Set up for Develop tab
                developImage = selectedImage;
                //Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(new MagickImage(developImage.Path));
                ResetSliders();
                //UpdateDevelopImage();
            }
        }

        private void Develop_Slider_Changed(object sender, DragCompletedEventArgs e)
        {
            if (sender is Slider slider)
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
            if (developImage == null) return;
            var adjusted = RawImageHelpers.ApplyAdjustments(
                new MagickImage(developImage.Path),
                developImageParams
            );
            Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(adjusted);
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