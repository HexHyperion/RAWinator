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
            RawContext raw = RawContext.OpenFile("C:\\Users\\szymo\\source\\repos\\HexHyperion\\rawinator\\img\\DSC6717.NEF");
            ProcessedImage thumb = raw.ExportThumbnail();
            Console.WriteLine("hej 1");
            Bitmap bmp = (Bitmap)Bitmap.FromStream(new MemoryStream(thumb.AsSpan<byte>().ToArray()));
            Console.WriteLine("hej 2");
            imaze.Source = BitmapToImageSource(bmp);
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }
}