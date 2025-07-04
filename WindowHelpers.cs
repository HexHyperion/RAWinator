﻿using System.Windows;
using System.Windows.Controls;

namespace rawinator
{
    class WindowHelpers
    {
        // Helper method to display metadata in a column-based format
        public static void DisplayMetadata(StackPanel panel, RawImage image, bool isSummary)
        {
            double maxTagWidth = 0;
            void AddRow(string tag, string value)
            {
                var row = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                var tagBlock = new TextBlock {
                    Text = tag,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = System.Windows.TextAlignment.Right,
                    Margin = new Thickness(0, 0, 8, 0),
                    Width = maxTagWidth
                };
                var valueBlock = new TextBlock {
                    Text = value
                };
                row.Children.Add(tagBlock);
                row.Children.Add(valueBlock);
                panel.Children.Add(row);
            }
            panel.Children.Clear();

            if (isSummary)
            {
                var imageDimensions = image.GetMetadata(MetadataTagLists.ImageDimensions);

                var tagStrings = new List<string> {
                    "Filename:",
                    "Image Size:"
                };
                foreach (var tag in image.GetMetadata(MetadataTagLists.General))
                {
                    if (!string.IsNullOrEmpty(tag.Item1))
                        tagStrings.Add(tag.Item1 + ":");
                }
                maxTagWidth = 0;
                foreach (var tag in tagStrings)
                {
                    var tb = new TextBlock {
                        Text = tag,
                        FontWeight = FontWeights.Bold
                    };
                    tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    if (tb.DesiredSize.Width > maxTagWidth)
                        maxTagWidth = tb.DesiredSize.Width;
                }
                maxTagWidth += 4;

                AddRow("Filename:", image.Filename);
                AddRow("Image Size:", $"{imageDimensions[0].Item2} x {imageDimensions[1].Item2} pixels");
                panel.Children.Add(new Border { Height = 8 });

                foreach (var tag in image.GetMetadata(MetadataTagLists.General))
                {
                    if (string.IsNullOrEmpty(tag.Item1))
                    {
                        panel.Children.Add(new Border { Height = 8 });
                        continue;
                    }
                    AddRow(tag.Item1 + ":", tag.Item2);
                }
            }
            else
            {
                panel.Children.Clear();
                foreach (var directory in image.Metadata)
                {
                    var dirTitle = directory.Name;
                    var dirTitleBlock = new TextBlock {
                        Text = dirTitle,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 8, 0, 2)
                    };
                    panel.Children.Add(dirTitleBlock);

                    maxTagWidth = 0;
                    var dirTagNames = directory.Tags.Select(tag => tag.Name + ":").ToList();
                    foreach (var tag in dirTagNames)
                    {
                        var tb = new TextBlock {
                            Text = tag,
                            FontWeight = FontWeights.Bold
                        };
                        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        if (tb.DesiredSize.Width > maxTagWidth)
                        {
                            maxTagWidth = tb.DesiredSize.Width;
                        }
                    }
                    maxTagWidth += 4;

                    foreach (var tag in directory.Tags)
                    {
                        AddRow(tag.Name + ":", tag.Description ?? "");
                    }
                }
            }
        }


        // Crop and resize helpers
        public static void AdjustRectForAspect(ref double width, ref double height, double aspect)
        {
            double absW = Math.Abs(width), absH = Math.Abs(height);
            if (absW > absH * aspect)
            {
                width = Math.Sign(width) * absH * aspect;
                height = Math.Sign(height) * absH;
            }
            else
            {
                width = Math.Sign(width) * absW;
                height = Math.Sign(height) * (absW / aspect);
            }
        }

        public static void ClampRectToCanvas(ref double x, ref double y, ref double w, ref double h,
                                             double offsetX, double offsetY, double maxW, double maxH)
        {
            if (w < 0)
            {
                double minX = Math.Max(x + w, offsetX);
                w = x - minX;
                x = minX;
            }
            else
            {
                double maxX = Math.Min(x + w, offsetX + maxW);
                w = maxX - x;
            }

            if (h < 0)
            {
                double minY = Math.Max(y + h, offsetY);
                h = y - minY;
                y = minY;
            }
            else
            {
                double maxY = Math.Min(y + h, offsetY + maxH);
                h = maxY - y;
            }
        }

        public static void NormalizeRect(ref double pos, ref double size)
        {
            if (size < 0)
            {
                pos += size;
                size = -size;
            }
        }

        public static void ClampRectToBounds(ref double x, ref double y, ref double w, ref double h,
                                             double xMin, double yMin, double maxW, double maxH)
        {
            double xMax = xMin + maxW;
            double yMax = yMin + maxH;

            x = Math.Max(x, xMin);
            y = Math.Max(y, yMin);
            if (x + w > xMax) w = xMax - x;
            if (y + h > yMax) h = yMax - y;
        }
    }
}
