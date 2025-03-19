using Sdcb.LibRaw;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace rawinator
{
    static class RawImageHelpers
    {
        public static Bitmap ProcessedImageToBitmap(ProcessedImage rgbImage)
        {
            rgbImage.SwapRGB();
            using Bitmap bmp = new Bitmap(rgbImage.Width, rgbImage.Height, rgbImage.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, rgbImage.DataPointer);

            //// Correct the orientation based on EXIF data
            //if (rgbImage.Orientation != 1)
            //{
            //    RotateFlipType rotateFlipType = GetRotateFlipType(rgbImage.Orientation);
            //    bmp.RotateFlip(rotateFlipType);
            //}

            // Create a new bitmap with the correct size
            Bitmap resizedBmp = new Bitmap(bmp);

            return resizedBmp;
        }

        private static RotateFlipType GetRotateFlipType(int exifOrientation)
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