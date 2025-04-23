using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Sdcb.LibRaw;
using Sdcb.LibRaw.Natives;

namespace rawinator
{
    public class RawImage
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public ImageSource Thumbnail { get; set; }
        public RawContext Raw { get; set; }

        public RawImage() { }
        public RawImage(string path)
        {
            Path = path;
            Filename = System.IO.Path.GetFileName(path);
            Raw = RawContext.OpenFile(path);

            Raw.UnpackThumbnail();
            ProcessedImage fullSizePreview;
            // fullSizePreview = Raw.MakeDcrawMemoryImage();
            fullSizePreview = Raw.MakeDcrawMemoryThumbnail();

            fullSizePreview.SwapRGB();

            Bitmap previewBitmap = new Bitmap(
                fullSizePreview.Width,
                fullSizePreview.Height,
                fullSizePreview.Width * 3,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                fullSizePreview.DataPointer
            );


            var metadata = GetMetadata();
            var exifData = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var exifOrientation = exifData?.GetDescription(ExifDirectoryBase.TagOrientation) ?? "1";
            previewBitmap.RotateFlip(RawImageHelpers.GetRotateFlipType(int.Parse(exifOrientation)));

            //// Messagebox each metadata directory
            //foreach (var directory in metadata)
            //{
            //    StringBuilder stringBuilder = new StringBuilder();
            //    foreach (var tag in directory.Tags)
            //    {
            //        stringBuilder.AppendLine($"{tag.Name}: {tag.Description}");
            //    }
            //    MessageBox.Show(stringBuilder.ToString(), directory.Name);
            //}

            // Messagebox orientation, but not from ExifSubIfdDirectory or exifOrientation
            string orientation = metadata.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDescription(ExifDirectoryBase.TagOrientation) ?? "";
            MessageBox.Show($"Orientation: {orientation}", "Orientation");

            // Find number in the orientation string (there is only one, but it can be in the middle of the string) symbolising angle
            int angle = 0;
            try
            {
                angle = int.Parse(new string(orientation.Where(char.IsDigit).ToArray()));
            }
            catch {}
             MessageBox.Show($"Angle: {angle}", "Angle");

            Thumbnail = RawImageHelpers.BitmapToImageSource(previewBitmap);
        }


        public ProcessedImage GetProcessedImage()
        {
            Raw.Unpack();
            Raw.DcrawProcess();
            return Raw.MakeDcrawMemoryImage();
        }

        public string GetMetadataString()
        {
            LibRawImageParams imageParams = Raw.ImageParams;
            LibRawImageOtherParams otherParams = Raw.ImageOtherParams;
            LibRawLensInfo lensInfo = Raw.LensInfo;
            return $"Filename: {Filename}\n" +
                   $"Date: {DateTimeOffset.FromUnixTimeSeconds(otherParams.Timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                   $"Artist: {otherParams.Artist}\n\n" +
                   $"Camera: {imageParams.Make} {imageParams.Model}\n" +
                   $"Lens: {lensInfo.LensMake} {lensInfo.Lens}\n" +
                   $"Focal length: {otherParams.FocalLength}mm\n\n" +
                   $"ISO: {otherParams.IsoSpeed}\n" +
                   $"Aperture: f/{otherParams.Aperture}\n" +
                   $"Shutter speed: {(otherParams.Shutter < 1 ? $"1/{1 / otherParams.Shutter:F0}" : otherParams.Shutter)} s\n";
        }

        public IEnumerable<Directory> GetMetadata()
        {
            return ImageMetadataReader.ReadMetadata(Path);
        }
    }
}