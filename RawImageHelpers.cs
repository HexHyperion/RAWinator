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
            double strength = adjustment / 75.0;
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
            var editedImage = baseImage.Clone();

            // ===== Exposure =====
            // Each stop of exposure means 2x more light
            if (developSettings.Exposure != 0)
            {
                double exposureFactor = Math.Pow(2, developSettings.Exposure);
                editedImage.Evaluate(Channels.RGB, EvaluateOperator.Multiply, exposureFactor);
            }

            // ===== Contrast =====
            editedImage.BrightnessContrast(new Percentage(0), new Percentage(developSettings.Contrast));

            // ===== White balance - temp/tint =====
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

            // ===== Brightness, saturation and hue =====
            editedImage.Modulate(
                new Percentage(100 + developSettings.Brightness),
                new Percentage(100 + developSettings.Saturation),
                new Percentage(100 + developSettings.Hue / 1.8)
            );


            // ===== Highlights and shadows =====
            if (developSettings.Shadows != 0)
            {
                var shadowCoefficients = GeneratePolynomialCoefficients((int)developSettings.Shadows, true);
                using var shadowsAdjusted = ApplyPolynomialFunction((MagickImage)editedImage, shadowCoefficients);
                editedImage.Composite(shadowsAdjusted, CompositeOperator.Over);
            }
            if (developSettings.Highlights != 0)
            {
                var highlightCoefficients = GeneratePolynomialCoefficients((int)developSettings.Highlights, false);
                using var highlightsAdjusted = ApplyPolynomialFunction((MagickImage)editedImage, highlightCoefficients);
                editedImage.Composite(highlightsAdjusted, CompositeOperator.Over);
            }            


            // ===== Per-color HSL adjustments =====
            var perColor = developSettings.PerColor;
            bool anyPerColor = perColor.Hue.Values.Any(v => v != 0) ||
                               perColor.Saturation.Values.Any(v => v != 0) ||
                               perColor.Luminance.Values.Any(v => v != 0);
            if (anyPerColor)
            {
                editedImage.ColorSpace = ColorSpace.sRGB;
                const double minSaturationForColorAdjust = 0.05; // Only affect saturated colors
                using (var pixels = editedImage.GetPixels())
                {
                    int width = editedImage.Width;
                    int height = editedImage.Height;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var pixel = pixels.GetPixel(x, y);
                            double red = pixel.GetChannel(0) / 65535.0;
                            double green = pixel.GetChannel(1) / 65535.0;
                            double blue = pixel.GetChannel(2) / 65535.0;
                            RgbToHsl(red, green, blue, out double hue, out double sat, out double lum);

                            if (sat > minSaturationForColorAdjust)
                            {
                                // Find which color range this pixel's hue falls into
                                foreach (var kvp in HslColorRanges.HueRanges)
                                {
                                    var color = kvp.Key;
                                    var range = kvp.Value;
                                    double hue360 = (hue + 360) % 360;
                                    if (range.Contains(hue360))
                                    {
                                        double hueAdj = perColor.Hue[color];
                                        double satAdj = perColor.Saturation[color];
                                        double lumAdj = perColor.Luminance[color];

                                        if (hueAdj != 0 || satAdj != 0 || lumAdj != 0)
                                        {
                                            // Matching global adjustments' scales
                                            hue = (hue + (hueAdj / 1.8) + 360) % 360;
                                            sat = Math.Clamp(sat * ((100 + satAdj) / 100.0), 0, 1);
                                            lum = Math.Clamp(lum * ((100 + lumAdj) / 100.0), 0, 1);
                                        }
                                        break;
                                    }
                                }
                            }

                            HslToRgb(hue, sat, lum, out double nr, out double ng, out double nb);

                            pixel.SetChannel(0, (ushort)Math.Clamp(nr * 65535, 0, 65535));
                            pixel.SetChannel(1, (ushort)Math.Clamp(ng * 65535, 0, 65535));
                            pixel.SetChannel(2, (ushort)Math.Clamp(nb * 65535, 0, 65535));
                        }
                    }
                }
            }

            editedImage.AutoOrient();
            editedImage.Strip();

            return (MagickImage)editedImage;
        }

        // HSL conversion helpers
        // r,g,b in [0,1], h in [0,360), s,l in [0,1]
        private static void RgbToHsl(double red, double green, double blue, out double hue, out double saturation, out double luminance)
        {
            double max = Math.Max(red, Math.Max(green, blue));
            double min = Math.Min(red, Math.Min(green, blue));
            luminance = (max + min) / 2.0;

            if (max == min)
            {
                hue = 0;
                saturation = 0;
            }
            else
            {
                double diff = max - min;
                saturation = luminance > 0.5 ? diff / (2.0 - max - min) : diff / (max + min);

                if (max == red)
                    hue = 60 * (((green - blue) / diff) % 6);
                else if (max == green)
                    hue = 60 * (((blue - red) / diff) + 2);
                else
                    hue = 60 * (((red - green) / diff) + 4);

                if (hue < 0) hue += 360;
            }
        }

        private static void HslToRgb(double hue, double saturation, double luminance, out double red, out double green, out double blue)
        {
            hue = hue % 360;
            if (saturation == 0)
            {
                red = green = blue = luminance;
            }
            else
            {
                double q = luminance < 0.5 ? luminance * (1 + saturation) : luminance + saturation - luminance * saturation;
                double p = 2 * luminance - q;
                red = HueToRgb(p, q, hue + 120);
                green = HueToRgb(p, q, hue);
                blue = HueToRgb(p, q, hue - 120);
            }
        }

        private static double HueToRgb(double p, double q, double hueOffset)
        {
            hueOffset = (hueOffset + 360) % 360;
            if (hueOffset < 60) return p + (q - p) * hueOffset / 60;
            if (hueOffset < 180) return q;
            if (hueOffset < 240) return p + (q - p) * (240 - hueOffset) / 60;
            return p;
        }
    }
}