using ImageMagick;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

        public static MagickImage ApplyAdjustments(
            MagickImage baseImage,
            RawImageProcessParams developSettings)
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

            // Shadows - right now they literally work like highlights in LR, like wtf
            // Probably need to adjust the numbers a bit
            if (developSettings.Shadows != 0)
            {
                using (var shadowMask = maskBaseImage.Clone())
                {
                    shadowMask.ColorSpace = ColorSpace.Gray;
                    // Use Quantum.Max directly for 16-bit, no cast to byte
                    shadowMask.Level(0, (ushort)(Quantum.Max * 0.5));
                    shadowMask.SigmoidalContrast(6, 0.25, Channels.Gray);

                    using (var shadowsAdjusted = editedImage.Clone())
                    {
                        var modValue = 100 + developSettings.Shadows;
                        shadowsAdjusted.Modulate(new Percentage(modValue), new Percentage(100), new Percentage(100));

                        shadowsAdjusted.Composite(shadowMask, CompositeOperator.CopyAlpha);
                        editedImage.Composite(shadowsAdjusted, CompositeOperator.Over);
                    }
                }
            }

            // Highlights - seems like they work only on *really* bright areas rn
            if (developSettings.Highlights != 0)
            {
                using (var highlightMask = maskBaseImage.Clone())
                {
                    highlightMask.ColorSpace = ColorSpace.Gray;
                    highlightMask.Level((ushort)(Quantum.Max * 0.5), Quantum.Max);
                    highlightMask.SigmoidalContrast(6, 0.75, Channels.Gray);

                    using (var highlightsAdjusted = editedImage.Clone())
                    {
                        var modValue = 100 - developSettings.Highlights;
                        highlightsAdjusted.Modulate(new Percentage(modValue), new Percentage(100), new Percentage(100));

                        highlightsAdjusted.Composite(highlightMask, CompositeOperator.CopyAlpha);
                        editedImage.Composite(highlightsAdjusted, CompositeOperator.Over);
                    }
                }
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