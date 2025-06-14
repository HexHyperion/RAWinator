﻿using ImageMagick;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
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

            // ===== Crop =====
            if (developSettings.CropWidth > 0 && developSettings.CropHeight > 0 &&
                (developSettings.CropWidth < 1 || developSettings.CropHeight < 1 ||
                 developSettings.CropX > 0 || developSettings.CropY > 0))
            {
                int x = (int)Math.Round(developSettings.CropX * editedImage.Width);
                int y = (int)Math.Round(developSettings.CropY * editedImage.Height);
                uint w = (uint)Math.Round(developSettings.CropWidth * editedImage.Width);
                uint h = (uint)Math.Round(developSettings.CropHeight * editedImage.Height);

                x = (int)Math.Clamp(x, 0, editedImage.Width - 1);
                y = (int)Math.Clamp(y, 0, editedImage.Height - 1);
                w = (uint)Math.Clamp(w, 1, editedImage.Width - x);
                h = (uint)Math.Clamp(h, 1, editedImage.Height - y);

                editedImage.Crop(new MagickGeometry(x, y, w, h));
                editedImage.ResetPage();
            }


            // ===== Auto effects =====
            if (developSettings.UseEnhance)
            {
                editedImage.Enhance();
            }
            if (developSettings.UseDenoise)
            {
                editedImage.ReduceNoise();
            }
            if (developSettings.UseAutoGamma)
            {
                editedImage.AutoGamma();
            }
            if (developSettings.UseAutoLevel)
            {
                editedImage.AutoLevel();
            }


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
                var highlightCoefficients = GeneratePolynomialCoefficients((int)-developSettings.Highlights, false);
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
                const double minSaturationForColorAdjust = 0.05;

                int width = (int)editedImage.Width;
                int height = (int)editedImage.Height;

                // Precompute hue-to-color mapping
                var hueToColorMap = new HslColorRange?[360];
                foreach (var kvp in HslColorRanges.HueRanges)
                {
                    var color = kvp.Key;
                    var range = kvp.Value;
                    for (int i = 0; i < 360; i++)
                        if (range.Contains(i))
                            hueToColorMap[i] = color;
                }

                using var pixels = editedImage.GetPixelsUnsafe();
                Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, y =>
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
                            int hueIndex = (int)((hue + 360) % 360);
                            if (hueToColorMap[hueIndex] is HslColorRange color)
                            {
                                double hueAdj = perColor.Hue[color];
                                double satAdj = perColor.Saturation[color];
                                double lumAdj = perColor.Luminance[color];

                                if (hueAdj != 0 || satAdj != 0 || lumAdj != 0)
                                {
                                    hue = (hue + hueAdj / 1.8 + 360) % 360;
                                    sat = Math.Clamp(sat * ((100 + satAdj) / 100), 0, 1);
                                    lum = Math.Clamp(lum * ((100 + lumAdj) / 100), 0, 1);

                                    HslToRgb(hue, sat, lum, out double newRed, out double newGreen, out double newBlue);

                                    pixel.SetChannel(0, (ushort)(newRed * ushort.MaxValue));
                                    pixel.SetChannel(1, (ushort)(newGreen * ushort.MaxValue));
                                    pixel.SetChannel(2, (ushort)(newBlue * ushort.MaxValue));

                                    pixels.SetPixel(pixel);
                                }
                            }
                        }
                    }
                });
            }


            // ===== Sharpening / Blur =====
            if (developSettings.Sharpness != 0)
            {
                if (developSettings.Sharpness != 0)
                {
                    double absSharp = Math.Abs(developSettings.Sharpness);
                    double radius = 2.0 + absSharp / 30.0;
                    double sigma = 0.5 + absSharp / 20.0;

                    if (developSettings.Sharpness < 0)
                    {
                        editedImage.Blur(radius, sigma);
                    }
                    else
                    {
                        editedImage.Sharpen(radius, sigma);
                    }
                }
            }

            // ===== Vignette =====
            if (developSettings.Vignette != 0)
            {
                uint width = editedImage.Width;
                uint height = editedImage.Height;
                double centerX = width / 2.0;
                double centerY = height / 2.0;

                // -100 = strong darkening, +100 = strong lightening
                double vignetteStrength = -developSettings.Vignette / 100.0 * 0.85;

                using var pixels = editedImage.GetPixelsUnsafe();
                ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
                Parallel.For(0, height, parallelOptions, y => {
                    for (int x = 0; x < width; x++)
                    {
                        double distX = (x - centerX) / centerX;
                        double distY = (y - centerY) / centerY;
                        double dist = Math.Sqrt(distX * distX + distY * distY); // Give some love to Pythagoras

                        double factor = 1.0 - dist * vignetteStrength;

                        var pixel = pixels.GetPixel(x, (int)y);
                        for (uint channel = 0; channel < 3; channel++)
                        {
                            double val = pixel.GetChannel(channel) / 65535.0;
                            val = Math.Clamp(val * factor, 0, 1);
                            pixel.SetChannel(channel, (ushort)(val * 65535));
                        }
                    }
                });
            }

            // ===== Noise =====
            if (developSettings.Noise != 0)
            {
                double noiseStrength = developSettings.Noise / 150.0;
                editedImage.AddNoise(NoiseType.Gaussian, noiseStrength);
            }


            // ===== Special effects =====
            if (developSettings.UseGrayscale)
            {
                editedImage.Grayscale();
            }
            if (developSettings.UseSepia)
            {
                editedImage.SepiaTone();
            }
            if (developSettings.UseSolarize)
            {
                editedImage.Solarize();
            }
            if (developSettings.UseInvert)
            {
                editedImage.Negate();
            }
            if (developSettings.UseOilPaint)
            {
                editedImage.OilPaint(5, 20);
            }
            if (developSettings.UseCharcoal)
            {
                editedImage.Charcoal();
            }
            if (developSettings.UsePosterize)
            {
                editedImage.Posterize(5);
            }
            if (developSettings.UseSketch)
            {
                editedImage.Sketch();
            }


            // ===== Border =====
            if (developSettings.BorderWidth != 0)
            {
                MagickColor borderColor = MagickColors.White;
                try
                {
                    string hex = developSettings.BorderColor ?? "#ffffff";
                    if (hex.StartsWith('#'))
                    {
                        hex = hex[1..];
                    }
                    if (hex.Length == 3) // #RGB
                    {
                        borderColor = new MagickColor(
                            (ushort)(Convert.ToUInt16(new string(hex[0], 2), 16) * 257),
                            (ushort)(Convert.ToUInt16(new string(hex[1], 2), 16) * 257),
                            (ushort)(Convert.ToUInt16(new string(hex[2], 2), 16) * 257)
                        );
                    }
                    else if (hex.Length == 6) // #RRGGBB
                    {
                        borderColor = new MagickColor(
                            (ushort)(Convert.ToUInt16(hex[..2], 16) * 257),
                            (ushort)(Convert.ToUInt16(hex.Substring(2, 2), 16) * 257),
                            (ushort)(Convert.ToUInt16(hex.Substring(4, 2), 16) * 257)
                        );
                    }
                    else if (hex.Length == 8) // #AARRGGBB
                    {
                        borderColor = new MagickColor(
                            (ushort)(Convert.ToUInt16(hex.Substring(2, 2), 16) * 257),
                            (ushort)(Convert.ToUInt16(hex.Substring(4, 2), 16) * 257),
                            (ushort)(Convert.ToUInt16(hex.Substring(6, 2), 16) * 257),
                            (ushort)(Convert.ToUInt16(hex[..2], 16) * 257)
                        );
                    }
                }
                catch
                {
                    borderColor = MagickColors.White;
                }
                editedImage.BorderColor = borderColor;
                editedImage.Border(developSettings.BorderWidth);
            }

            editedImage.AutoOrient();

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
            hue %= 360;
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