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
using Sdcb.LibRaw;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Sdcb.LibRaw.Natives;
using Microsoft.Win32;
using System.Collections;

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
                    MessageBox.Show(rawImage.Filename);
                    Library_Image_Thumbnail.Source = RawImageHelpers.BitmapToImageSource(rawImage.Thumbnail);
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