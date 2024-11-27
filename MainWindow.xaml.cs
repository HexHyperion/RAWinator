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

namespace rawinator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RawContext r = RawContext.OpenFile("C:\\Users\\szymo\\source\\repos\\HexHyperion\\rawinator\\img\\DSC6717.NEF");

        public MainWindow()
        {
            InitializeComponent();

            r.Unpack();
            r.DcrawProcess();
            ProcessedImage image = r.MakeDcrawMemoryImage();
            Bitmap bmp = ProcessedImageToBitmap(image);
            imaze.Source = BitmapToImageSource(bmp);

            LibRawImageParams imageParams = r.ImageParams;
            LibRawImageOtherParams otherParams = r.ImageOtherParams;
            LibRawLensInfo lensInfo = r.LensInfo;

            meta.Content += ($"Camera: {imageParams.Model}\n");
            meta.Content += ($"Version: {imageParams.Software}\n");
            meta.Content += ($"ISO: {otherParams.IsoSpeed}\n");
            meta.Content += ($"Shutter Speed: 1/{1 / otherParams.Shutter:F0}s\n");
            meta.Content += ($"Focal Length: {otherParams.FocalLength}mm\n");
            meta.Content += ($"Artist Tag: {otherParams.Artist}\n");
            meta.Content += ($"Shot Date: {new DateTime(1970, 1, 1, 8, 0, 0).AddSeconds(otherParams.Timestamp)}\n");
            meta.Content += ($"Lens Name: {lensInfo.Lens}");
        }

        Bitmap ProcessedImageToBitmap(ProcessedImage rgbImage)
        {
            rgbImage.SwapRGB();
            using Bitmap bmp = new Bitmap(rgbImage.Width, rgbImage.Height, rgbImage.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, rgbImage.DataPointer);
            return new Bitmap(bmp);
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

        public static Bitmap ByteToImage(byte[] blob)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                mStream.Write(blob, 0, blob.Length);
                mStream.Seek(0, SeekOrigin.Begin);

                Bitmap bm = new Bitmap(mStream);
                return bm;
            }
        }


        private void expo_Click(object sender, RoutedEventArgs e)
        {
            r.DcrawProcess(c =>
            {
                c.HalfSize = true;
                c.Brightness = 2.5F;
            });
            using ProcessedImage rgbImage = r.MakeDcrawMemoryImage();
            Bitmap bmp = ProcessedImageToBitmap(rgbImage);
            imaze.Source = BitmapToImageSource(bmp);
        }

        private void export_Click(object sender, RoutedEventArgs e)
        {
            r.SaveRawImage("C:\\Users\\szymo\\Desktop\\DSC6717.tiff");
        }
    }
}