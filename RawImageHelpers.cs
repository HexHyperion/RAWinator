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
            RawImageProcessParams developSettings)
        {
            var editedImage = baseImage.Clone();

            // Exposure - really just brightness adjustment for now, TODO implement proper exposure adjustment
            editedImage.Evaluate(Channels.RGB, EvaluateOperator.Add, developSettings.Exposure);

            // Contrast
            if (developSettings.Contrast > 0)
            {
                for (int i = 0; i < (int)developSettings.Contrast; i++)
                {
                    editedImage.Contrast();
                }
            }
            else if (developSettings.Contrast < 0)
            {
                for (int i = 0; i < (int)(-developSettings.Contrast); i++)
                {
                    editedImage.InverseContrast();
                }
            }

            // Saturation
            editedImage.Modulate((Percentage)100, (Percentage)(100 + developSettings.Saturation), (Percentage)100);

            // White balance (temperature)
            if (developSettings.Temperature != 0)
            {
                editedImage.Evaluate(Channels.Red, EvaluateOperator.Add, developSettings.Temperature);
                editedImage.Evaluate(Channels.Blue, EvaluateOperator.Subtract, developSettings.Temperature);
            }
            if (developSettings.TemperatureTint != 0)
            {
                editedImage.Evaluate(Channels.Blue, EvaluateOperator.Add, developSettings.TemperatureTint);
                editedImage.Evaluate(Channels.Green, EvaluateOperator.Subtract, developSettings.TemperatureTint);
            }

            // Shadows - right now they literally work like highlights in LR, like wtf
            // Probably need to adjust the numbers a bit
            if (developSettings.Shadows != 0)
            {
                using (var shadowMask = editedImage.Clone())
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
                using (var highlightMask = editedImage.Clone())
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

            return (MagickImage)editedImage;
        }
    }
}