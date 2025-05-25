using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using ImageMagick;
using ImageMagick.Formats;
using MetadataExtractor.Formats.Exif.Makernotes;
using System.Text;
using System.Windows;

namespace rawinator
{
    public class RawImage
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public MagickImage FullImage { get; set; }
        public ImageSource Thumbnail { get; set; }
        public IEnumerable<Directory> Metadata { get; set; }

        public RawImage() { }
        public RawImage(string path)
        {
            Path = path;
            Filename = System.IO.Path.GetFileName(path);

            var defines = new DngReadDefines
            { 
                ReadThumbnail = true,
                UseCameraWhitebalance = true,
                DisableAutoBrightness = true,
                InterpolationQuality = DngInterpolation.Ahd
            };
            FullImage = new MagickImage();
            FullImage.Settings.SetDefines(defines);
            FullImage.Read(path);
            FullImage.AutoOrient();
            Metadata = ImageMetadataReader.ReadMetadata(Path);

            var thumbnailData = FullImage.GetProfile("dng:thumbnail")?.ToByteArray();
            if (thumbnailData != null && thumbnailData.Length > 0)
            {
                using var thumbnailImage = new MagickImage(thumbnailData);
                thumbnailImage.AutoOrient();
                Thumbnail = RawImageHelpers.MagickImageToBitmapImage(thumbnailImage);
            }

            //// Messagebox each metadata directory
            //foreach (var directory in Metadata)
            //{
            //    StringBuilder stringBuilder = new StringBuilder();
            //    foreach (var tag in directory.Tags)
            //    {
            //        stringBuilder.AppendLine($"{tag.Name}: {tag.Description}");
            //    }
            //    MessageBox.Show(stringBuilder.ToString(), directory.Name);
            //}
        }

        public string GetMetadataString()
        {
            return "no metadata for you o.o";
        }
    }
}