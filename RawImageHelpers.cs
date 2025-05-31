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
                editedImage.Evaluate(Channels.Red, EvaluateOperator.Add, developSettings.WbTemperature);
                editedImage.Evaluate(Channels.Blue, EvaluateOperator.Subtract, developSettings.WbTemperature);
            }
            if (developSettings.WbTint != 0)
            {
                editedImage.Evaluate(Channels.Blue, EvaluateOperator.Add, developSettings.WbTint);
                editedImage.Evaluate(Channels.Green, EvaluateOperator.Subtract, developSettings.WbTint);
            }

            // Shadows - right now they literally work like highlights in LR, like wtf
            // Probably need to adjust the numbers a bit
            if (developSettings.Shadows != 0)
            {
                using (var shadowMask = maskBaseImage.Clone())
                {
                    shadowMask.ColorSpace = ColorSpace.Gray;
                    shadowMask.Level(0, (byte)(Quantum.Max * 0.5));
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
                    highlightMask.Level((byte)(Quantum.Max * 0.5), Quantum.Max);
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