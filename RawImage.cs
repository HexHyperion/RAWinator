﻿using ImageMagick;
using ImageMagick.Formats;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Windows.Media;

namespace rawinator
{
    public class RawImage
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public ImageSource? SmallThumbnail { get; set; }
        public IEnumerable<Directory> Metadata { get; set; }
        public RawImageProcessParams ProcessParams { get; set; }

        public RawImage(string path)
        {
            Path = path;
            Filename = System.IO.Path.GetFileName(path);
            Metadata = ImageMetadataReader.ReadMetadata(Path);
            ProcessParams = new();

            var defines = new DngReadDefines { ReadThumbnail = true };
            using var image = new MagickImage();
            image.Settings.SetDefines(defines);
            image.Ping(Path);

            var thumbnailData = image.GetProfile("dng:thumbnail")?.ToByteArray();
            if (thumbnailData != null && thumbnailData.Length > 0)
            {
                using var thumbnailImage = new MagickImage(thumbnailData);
                thumbnailImage.AutoOrient();

                // Resize so the longest edge is at most 500px
                uint width = thumbnailImage.Width;
                uint height = thumbnailImage.Height;
                uint maxEdge = Math.Max(width, height);
                if (maxEdge > 500)
                {
                    double scale = 500.0 / maxEdge;
                    uint newWidth = (uint)Math.Round(width * scale);
                    uint newHeight = (uint)Math.Round(height * scale);
                    thumbnailImage.Resize(newWidth, newHeight);
                }

                SmallThumbnail = RawImageHelpers.MagickImageToBitmapImage(thumbnailImage);
            }
        }

        public ImageSource? GetFullThumbnail()
        {
            var defines = new DngReadDefines { ReadThumbnail = true };
            using var image = new MagickImage();
            image.Settings.SetDefines(defines);
            image.Ping(Path);

            var thumbnailData = image.GetProfile("dng:thumbnail")?.ToByteArray();
            if (thumbnailData != null && thumbnailData.Length > 0)
            {
                using var thumbnailImage = new MagickImage(thumbnailData);
                thumbnailImage.AutoOrient();

                return RawImageHelpers.MagickImageToBitmapImage(thumbnailImage);
            }
            return null;
        }

        public MagickImage GetRawImage()
        {
            var image = new MagickImage();
            DngReadDefines defines = new() {
                UseAutoWhiteBalance = false,
                DisableAutoBrightness = false,
                UseCameraWhiteBalance = true,
                InterpolationQuality = DngInterpolation.ModifiedAhd
            };
            image.Settings.SetDefines(defines);
            image.Read(Path);
            image.AutoOrient();
            return image;
        }

        public List<(string, string)> GetMetadata(int?[] tags)
        {
            var result = new List<(string, string)>();
            foreach (var tag in tags)
            {
                if (tag != null)
                {
                    var tagName = Metadata.Select(d => d.GetTagName((int)tag)).FirstOrDefault() ?? "-";
                    if (tag == ExifDirectoryBase.TagImageHeight || tag == ExifDirectoryBase.TagImageWidth)
                    {
                        // Special handling for image dimensions to return the maximum value,
                        // as some cameras have multiple values for different thumbnails.
                        var tagDescription = Metadata
                            .Select(d => d.GetDescription((int)tag))
                            .Where(desc => !string.IsNullOrEmpty(desc))
                            .Select(desc => {
                                var firstPart = desc!.Split(' ')[0];
                                if (int.TryParse(firstPart, out int value))
                                    return value;
                                return (int?)null;
                            })
                            .Where(val => val.HasValue)
                            .Max();

                        result.Add((tagName, tagDescription?.ToString() ?? "-"));
                    }
                    else
                    {
                        var tagDescription = Metadata
                            .Select(d => d.GetDescription((int)tag))
                            .FirstOrDefault(desc =>
                                !string.IsNullOrEmpty(desc)
                            );

                        result.Add((tagName, tagDescription ?? "-"));
                    }
                }
                else
                {
                    result.Add(("", ""));
                }
            }
            return result;
        }
    }
}