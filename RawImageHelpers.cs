using ImageMagick;
using System.IO;
using System.Windows.Media.Imaging;

namespace rawinator
{
    static class RawImageHelpers
    {
        public static BitmapImage MagickImageToBitmapImage(MagickImage image)
        {
            using MemoryStream ms = new();
            image.Write(ms, MagickFormat.Bmp);
            ms.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            bitmapImage.StreamSource = null;
            bitmapImage.Freeze();
            return bitmapImage;
        }


        private static double[] GeneratePolynomialCoefficients(int adjustment, bool isShadow)
        {
            // Visualization on https://www.desmos.com/calculator/i0slukhnjj
            double strength = adjustment / 65.0;
            if (isShadow)
            {
                // [a, b, c, d, e] are coefficients for a polynomial function
                // ax^4 + bx^3 + cx^2 + dx + e
                return [-2 * strength, 5 * strength, -4 * strength, 1 + strength, 0];
            }
            else
            {
                return [2 * strength, -3 * strength, strength, 1, 0];
            }
        }

        private static MagickImage ApplyPolynomialFunction(MagickImage image, double[] coefficients)
        {
            var result = image.Clone();
            result.Evaluate(Channels.All, EvaluateFunction.Polynomial, coefficients);
            return (MagickImage)result;
        }

        public static MagickImage ApplyAdjustments(MagickImage baseImage, RawImageProcessParams developSettings)
        {
            // Clone the base image for highlight/shadow masking before exposure is applied
            using var maskBaseImage = baseImage.Clone();
            var editedImage = baseImage.Clone();

            // Brightness, saturation and hue
            editedImage.Modulate(
                new Percentage(100 + developSettings.Brightness),
                new Percentage(100 + developSettings.Saturation),
                new Percentage(100 + developSettings.Hue / 1.8)
            );

            // Contrast
            editedImage.BrightnessContrast(new Percentage(0), new Percentage(developSettings.Contrast));

            // White balance
            if (developSettings.WbTemperature != 0)
            {
                // +100 = warm, -100 = cool
                // Scales to about 0.5x to 1.5x red/blue channels
                double tempScale = 1.0 + (developSettings.WbTemperature / 100.0) * 0.5;
                // Clamp for safety
                tempScale = Math.Clamp(tempScale, 0.5, 1.5);
                editedImage.Evaluate(Channels.Red, EvaluateOperator.Multiply, tempScale);
                editedImage.Evaluate(Channels.Blue, EvaluateOperator.Multiply, 2.0 - tempScale);
            }
            if (developSettings.WbTint != 0)
            {
                // +100 = green, -100 = magenta
                double tintScale = 1.0 + (developSettings.WbTint / 100.0) * 0.5;
                tintScale = Math.Clamp(tintScale, 0.5, 1.5);
                editedImage.Evaluate(Channels.Green, EvaluateOperator.Multiply, tintScale);
            }

            // Shadows
            // todo use masks for everything and apply at return
            if (developSettings.Shadows != 0)
            {
                var shadowCoefficients = GeneratePolynomialCoefficients((int)developSettings.Shadows, true);
                using var shadowsAdjusted = ApplyPolynomialFunction((MagickImage)editedImage, shadowCoefficients);
                editedImage.Composite(shadowsAdjusted, CompositeOperator.Over);
            }

            // Highlights
            if (developSettings.Highlights != 0)
            {
                var highlightCoefficients = GeneratePolynomialCoefficients((int)developSettings.Highlights, false);
                using var highlightsAdjusted = ApplyPolynomialFunction((MagickImage)editedImage, highlightCoefficients);
                editedImage.Composite(highlightsAdjusted, CompositeOperator.Over);
            }

            // Exposure (apply last, so highlight/shadow masks are not affected by it)
            double exposureFactor = Math.Pow(2, developSettings.Exposure);
            editedImage.Evaluate(Channels.RGB, EvaluateOperator.Multiply, exposureFactor);

            editedImage.AutoOrient();
            editedImage.Strip();

            return (MagickImage)editedImage;
        }

    }
}