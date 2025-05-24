using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using ImageMagick;
using System.IO;
using System.Windows.Media.Imaging;

namespace rawinator
{
    static class RawImageHelpers
    {
        public static BitmapImage MagickImageToBitmapImage(MagickImage image)
        {
            using var ms = new MemoryStream();
            var bitmapImage = new BitmapImage();
            image.Write(ms, MagickFormat.Bmp);
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public static MagickImage ApplyAdjustments(
            MagickImage baseImage,
            double exposure,
            double highlights,
            double shadows,
            double temperature,
            double temperatureTint,
            double contrast,
            double saturation)
        {
            var img = baseImage.Clone();

            // Exposure
            img.Evaluate(Channels.RGB, EvaluateOperator.Add, exposure);

            // Contrast (broken rn)
            if (contrast > 0)
            {
                for (int i = 0; i < (int)contrast; i++)
                {
                    img.Contrast();
                }
            }
            else if (contrast < 0)
            {
                for (int i = 0; i < (int)(-contrast); i++)
                {
                    img.InverseContrast();
                }
            }

            // Saturation
            img.Modulate((Percentage)100, (Percentage)(100 + saturation), (Percentage)100);

            // White balance (temperature)
            if (temperature != 0)
            {
                img.Evaluate(Channels.Red, EvaluateOperator.Add, temperature);
                img.Evaluate(Channels.Blue, EvaluateOperator.Subtract, temperature);
            }
            if (temperatureTint != 0)
            {
                img.Evaluate(Channels.Blue, EvaluateOperator.Add, temperatureTint);
                img.Evaluate(Channels.Green, EvaluateOperator.Subtract, temperatureTint);
            }

            // Highlights/Shadows (broken rn, todo figure out)
            if (highlights != 0)
            {
                img.Level(0, (byte)(Quantum.Max - (int)highlights));
            }
            if (shadows != 0)
            {
                img.Level((byte)(int)shadows, Quantum.Max);
            }

            return (MagickImage)img;
        }
    }
}