using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Win32;
using System.Collections;
using ImageMagick;
using System.Windows.Controls.Primitives;

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

        public List<RawImage> importedImages { get; set; } = new List<RawImage>();
        public RawImage? selectedImage { get; set; } = null;
        private RawImage? developImage = null;
        private RawImageProcessParams developImageParams = new(0, 0, 0, 0, 0, 0, 0);

        private void Library_Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "RAW files|*.arw;*.cr2;*.cr3;*.nef;*.nrw;*.orf;*.pef;*.raf;*.rw2;*.srw;*.dng;*.k25;*.kdc;*.srf;*.sr2;*.mos;*.3fr;*.fff;*.rwl;*.iiq";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    Library_Image_List.Items.Add(file);
                    RawImage rawImage = new RawImage(file);
                    importedImages.Add(rawImage);
                    Library_Image_Thumbnail.Source = rawImage.Thumbnail;
                    Library_Image_Metadata.Content = rawImage.GetMetadataString();
                }
                if (Library_Image_List.SelectedItem == null && Library_Image_List.Items.Count > 0)
                {
                    Library_Image_List.SelectedIndex = Library_Image_List.Items.Count - 1;
                    selectedImage = importedImages[Library_Image_List.Items.Count-1];
                    developImage = selectedImage;
                }
            }
        }

        private void Library_Image_List_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Library_Image_List.SelectedItems != null)
            {
                int selectedNumber = Library_Image_List.SelectedItems.Count;
                if (selectedNumber > 1)
                {
                    MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete {selectedNumber} images from library? (files on disk won't be modified)", "Delete images", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        IList itemsToDelete = Library_Image_List.SelectedItems.Cast<string>().ToList();
                        foreach (string file in itemsToDelete)
                        {
                            Library_Image_List.Items.Remove(file);
                        }
                    }
                }
                else
                {
                    Library_Image_List.Items.Remove(Library_Image_List.SelectedItem);
                }
            }
        }

        private void Library_Image_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Library_Image_List.SelectedItem != null)
            {
                string? selectedFile = Library_Image_List.SelectedItem.ToString();
                if (selectedFile == null) return;
                RawImage? selectedImage = importedImages.Find(x => x.Path == selectedFile);
                if (selectedImage == null) return;
                Library_Image_Thumbnail.Source = selectedImage.Thumbnail;
                Library_Image_Metadata.Content = selectedImage.GetMetadataString();

                // Set up for Develop tab
                developImage = selectedImage;
                ResetSliders();
                UpdateDevelopImage();
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
                    case nameof(Develop_Slider_Highlights):
                        developImageParams.Highlights = slider.Value;
                        break;
                    case nameof(Develop_Slider_Shadows):
                        developImageParams.Shadows = slider.Value;
                        break;
                    case nameof(Develop_Slider_WhiteBalance):
                        developImageParams.Temperature = slider.Value;
                        break;
                    case nameof(Develop_Slider_WhiteBalanceTint):
                        developImageParams.TemperatureTint = slider.Value;
                        break;
                    case nameof(Develop_Slider_Contrast):
                        developImageParams.Contrast = slider.Value;
                        break;
                    case nameof(Develop_Slider_Saturation):
                        developImageParams.Saturation = slider.Value;
                        break;
                }
                UpdateDevelopImage();
            }
        }

        private void UpdateDevelopImage()
        {
            if (developImage == null) return;
            var adjusted = RawImageHelpers.ApplyAdjustments(
                developImage.FullImage,
                developImageParams
            );
            Develop_Image.Source = RawImageHelpers.MagickImageToBitmapImage(adjusted);
        }

        private void ResetSliders()
        {
            Develop_Slider_Exposure.Value = 0;
            Develop_Slider_Highlights.Value = 0;
            Develop_Slider_Shadows.Value = 0;
            Develop_Slider_WhiteBalance.Value = 0;
            Develop_Slider_WhiteBalanceTint.Value = 0;
            Develop_Slider_Contrast.Value = 0;
            Develop_Slider_Saturation.Value = 0;
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