using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Sdcb.LibRaw;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace rawinator
{
    static class RawImageHelpers
    {

        public static Bitmap RawToBitmap(RawImage raw)
        {
            ProcessedImage rgbImage = raw.GetProcessedImage();
            rgbImage.SwapRGB();
            using Bitmap bmp = new Bitmap(rgbImage.Width, rgbImage.Height, rgbImage.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, rgbImage.DataPointer);

            var metadata = raw.GetMetadata();
            var exifData = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var exifOrientation = exifData?.GetDescription(ExifDirectoryBase.TagOrientation) ?? "1";
            bmp.RotateFlip(GetRotateFlipType(int.Parse(exifOrientation)));

            Bitmap resizedBmp = new Bitmap(bmp);
            return resizedBmp;
        }

        public static RotateFlipType GetRotateFlipType(int exifOrientation)
        {
            return exifOrientation switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.Rotate180FlipX,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone,
            };
        }

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
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
    }
}